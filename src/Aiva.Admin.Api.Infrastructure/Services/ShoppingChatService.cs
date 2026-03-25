using System.Text.Json;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Ardalis.Result;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class ShoppingChatService(
    IShoppingToolService shoppingToolService,
    IChatCompletionService chatService,
    IPromptTemplateService promptTemplateService,
    ISystemPromptService systemPromptService,
    IChatHistoryService chatHistoryService,
    IJsonExtractionService jsonExtractionService,
    IStandaloneQuestionService standaloneQuestionService,
    IJsonParseService jsonParseService,
    //IHtmlTableParserService htmlTableParserService,
    ILogger<ShoppingChatService> logger) : IShoppingChatService
{
  public async Task<Result<ShoppingChatResult>> ProcessShoppingChatAsync(
      Conversation conversation,
      string userMessage,
      string userName,
      string? additionalUserData,
      CancellationToken cancellationToken)
  {
    try
    {
      List<ProductSelectionItemDto>? productSelection = null;
      if (!string.IsNullOrWhiteSpace(additionalUserData))
      {
        var productSelectionPrompt = await systemPromptService.GetActivePromptContentAsync(
            SystemPromptKey.From("product_selection"),
            cancellationToken);

        var productSelectionMessage = promptTemplateService.ReplacePromptByKey(productSelectionPrompt, new ReplacePromptDto
        {
          AdditionalData = additionalUserData
        });

        var productSelectionResult = await chatService.GetCompletionAsync("", productSelectionMessage);

        var productDataJson = jsonExtractionService.ExtractJson(productSelectionResult);

        productSelection = jsonParseService.Parse<List<ProductSelectionItemDto>>(productDataJson);
      }

      // Get recent messages and chat history
      var recentMessages = conversation.GetRecentMessages();
      var chatHistory = await chatHistoryService.SerializeChatHistoryAsync(recentMessages.ToList());

      // For standalone question generation, replace visual context with extracted product keywords
      // so the AI never sees "image/picture/upload" framing
      var messageForStandalone = ExtractCleanMessageForStandalone(userMessage);

      // Generate standalone question
      var standaloneMessage = promptTemplateService.ReplacePromptByKey(
          PromptTemplates.GuidelinesForShoppingStandalone,
          new ReplacePromptDto
          {
            FullName = !string.IsNullOrWhiteSpace(userName) ? userName : "User",
            ChatInput = messageForStandalone,
            ChatHistory = chatHistory,
            AdditionalData = productSelection != null ? JsonSerializer.Serialize(productSelection) : null
          });

      var standaloneQuestionResult = await chatService.GetCompletionAsync(
          PromptTemplates.SmartStoreStandaloneQuestionSystem,
          standaloneMessage);

      if (string.IsNullOrWhiteSpace(standaloneQuestionResult))
      {
        logger.LogWarning("Chat service returned empty response for conversation {ConversationId}", conversation.Id);
        return Result.Error("No response received from chat service");
      }

      // Extract and parse JSON
      var jsonStandalone = jsonExtractionService.ExtractJson(standaloneQuestionResult);

      if (string.IsNullOrWhiteSpace(jsonStandalone))
      {
        logger.LogWarning("Failed to extract JSON from response for conversation {ConversationId}. Response: {Response}",
            conversation.Id, standaloneQuestionResult);
        return Result.Error("Unable to extract valid JSON from chat response");
      }

      var dataStandalone = standaloneQuestionService.ParseStandaloneQuestion(jsonStandalone);

      if (string.IsNullOrEmpty(dataStandalone.QueryString) || string.IsNullOrEmpty(dataStandalone.StandaloneQuestion))
      {
        return Result.Error("No valid query string or standalone question found in the response.");
      }

      // Check if tools are needed first (no Azure AI Search here)
      var toolPrompt = await systemPromptService.GetActivePromptContentAsync(
          SystemPromptKey.From("tool-selector"),
          cancellationToken);

      var toolPromptResult = promptTemplateService.ReplacePromptByKey(toolPrompt, new ReplacePromptDto
      {
        AdditionalData = productSelection != null ? JsonSerializer.Serialize(productSelection) : null
      });

      var availableTools = shoppingToolService.GetAvailableTools();

      var toolSelectionResult = await chatService.SelectToolsAsync(
          toolPromptResult,
          dataStandalone.StandaloneQuestion,
          availableTools,
          cancellationToken);

      // Get shopping assistant system prompt template
      var systemPromptTemplate = await systemPromptService.GetActivePromptContentAsync(
          SystemPromptKey.From("shopping-assistant"),
          cancellationToken);

      // Process tool execution if needed
      if (toolSelectionResult.IsSuccess && toolSelectionResult.Value.Any())
      {
        var toolResults = await ExecuteToolCallsAsync(
            toolSelectionResult.Value,
            userName,
            cancellationToken);

        var toolResultsInstruction = BuildToolResultsInstruction();

        var systemPrompt = promptTemplateService.ReplacePromptByKey(
          systemPromptTemplate,
          new ReplacePromptDto
          {
            FullName = !string.IsNullOrWhiteSpace(userName) ? userName : "User",
            ProductData = string.Join("\n", toolResults.Values),
            ToolResultsInstruction = toolResultsInstruction
          });

        var finalResponse = await chatService.GetCompletionAsync(systemPrompt, dataStandalone.StandaloneQuestion);

        var resultWithTool = new ShoppingChatResult
        {
          TextResponse = finalResponse,
          HasProducts = true,
          ToolsExecuted = toolSelectionResult.Value.Select(tc => tc.Function.Name).ToList(),
          ToolResults = toolResults,
        };

        // Detect checkout action from tool results
        DetectAndSetActions(resultWithTool, toolResults);

        return Result.Success(resultWithTool);
      }

      logger.LogInformation("No tools needed, using basic system prompt for conversation {ConversationId}", conversation.Id);

      // Create enhanced system prompt without Azure AI Search results
      var enhancedSystemPrompt = promptTemplateService.ReplacePromptByKey(
          systemPromptTemplate,
          new ReplacePromptDto
          {
            FullName = !string.IsNullOrWhiteSpace(userName) ? userName : "User",
          });

      // Get AI response without tools and without Azure AI Search
      var responseAnswer = await chatService.GetCompletionAsync(enhancedSystemPrompt, userMessage);

      if (string.IsNullOrWhiteSpace(responseAnswer))
      {
        logger.LogWarning("Chat service returned empty shopping response for conversation {ConversationId}", conversation.Id);
        return Result.Error("No shopping response received from chat service");
      }

      // Parse shopping response
      var result = ParseShoppingResponse(responseAnswer);

      logger.LogInformation("Successfully processed shopping chat for conversation {ConversationId}", conversation.Id);

      return Result.Success(result);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing shopping chat for conversation {ConversationId}", conversation.Id);
      return Result.Error($"Shopping chat processing failed: {ex.Message}");
    }
  }

  private static string BuildToolResultsInstruction()
  {
    return """
<tool_execution_context>
CRITICAL: Shopping tools (add_to_cart, remove_from_cart, checkout, get_cart etc.) have ALREADY been executed.
The <catalog_data> section below contains TOOL EXECUTION RESULTS (success/failure messages), NOT product catalog.

OVERRIDE - IGNORE ALL OTHER PROMPT RULES: When this block is present, do NOT say "I don't have enough information", "I could not find", or ask the user for product details. The action has been COMPLETED. Your ONLY task is to confirm based on catalog_data.

SPECIAL INSTRUCTIONS FOR CART DISPLAY (get_cart):
- If catalog_data contains "CART_TABLE_DATA_START" and "CART_TABLE_DATA_END", convert the ROW data into an HTML table (NOT markdown table)
- Include ALL columns with interactive elements: Select | Cart ID | Product | SKU | Quantity Controls | Unit Price | Attributes | Subtotal
- The Cart ID is essential for remove operations - always display it prominently
- Format as clean, valid HTML that can be rendered directly

INTERACTIVE ELEMENTS FOR CART TABLE:
- Selection Column: use a real checkbox element for each row, for example `<input type="checkbox" value="{cartId}" checked="checked">`
- Quantity Controls: use an editable number input field that users can directly type into or use browser increment/decrement arrows, for example `<input type="number" min="1" value="{quantity}" data-cart-id="{cartId}" style="width: 60px; padding: 4px; text-align: center;">`
- Action Buttons: use real remove button element, for example `<button type="button" onclick="removeCartItem({cartId})">🗑️ Remove</button>`
- Bulk Actions: At table bottom, show options like "Remove Selected" and "Update Quantities"

EXAMPLE CART TABLE FORMAT (HTML):
```
<table border="1" cellspacing="0" cellpadding="8" style="border-collapse: collapse; width: 100%;">
  <thead>
    <tr>
      <th>Select</th><th>Cart ID</th><th>Product</th><th>SKU</th><th>Quantity</th><th>Unit Price</th><th>Attributes</th><th>Subtotal</th><th>Actions</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><input type="checkbox" value="123" checked="checked"></td>
      <td>123</td>
      <td>iPhone 15 (ID: 15)</td>
      <td>IP15-128</td>
      <td style="text-align: center;">
        <input type="number" min="1" value="2" data-cart-id="123" style="width: 60px; padding: 4px; text-align: center;">
      </td>
      <td>$799.00</td>
      <td>128GB, Blue</td>
      <td>$1,598.00</td>
    </tr>
  </tbody>
</table>
```

- Include the cart summary below the table with totals
- Explain that users can:
  * Select items using checkboxes for bulk operations
  * Adjust quantities by directly editing the number input field (type new value or use browser arrows)
  * Remove individual items using the remove button
  * Use Cart ID for precise remove operations in voice commands
- CRITICAL: Always use `<input type="number">` for quantity controls so users can directly edit values inline
- Never use plain text quantity controls like "🔺 3 🔻" or "[+] 3 [-]" or separate increment/decrement buttons

STANDARD TOOL RESPONSES:
- For other tools: Summarize results in a brief, friendly confirmation (1-3 sentences)
- Do NOT display product tables, search for products, or recommend alternatives unless the user asks
- Match the user's language: if user asked in English, respond in English; if Vietnamese, respond in Vietnamese
- If all tools succeeded: confirm success concisely (e.g. English: "Successfully added to cart!", Vietnamese: "Đã thêm vào giỏ hàng thành công!")
- If any failed: acknowledge briefly and offer to help
- Keep response focused on the action performed

Remember: For cart display, prioritize table formatting with Cart IDs. For other actions, keep responses brief and confirmatory.
</tool_execution_context>

""";
  }

  private ShoppingChatResult ParseShoppingResponse(string responseAnswer)
  {
    var result = new ShoppingChatResult
    {
      TextResponse = responseAnswer,
      HasProducts = true
    };

    return result;
  }

  private async Task<Dictionary<string, object>> ExecuteToolCallsAsync(
      List<ToolCall> toolCalls,
      string userId,
      CancellationToken cancellationToken)
  {
    var results = new Dictionary<string, object>();

    foreach (var toolCall in toolCalls)
    {
      try
      {
        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(
            toolCall.Function.Arguments) ?? new Dictionary<string, object>();

        var result = await shoppingToolService.ExecuteToolAsync(
            toolCall.Function.Name,
            parameters,
            userId,
            cancellationToken);

        results[toolCall.Id] = result.IsSuccess ? result.Value : result.Errors;

        logger.LogInformation("Executed tool {FunctionName} with result: {Result}",
            toolCall.Function.Name, result.IsSuccess ? "Success" : "Failed");
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Failed to execute tool {FunctionName}", toolCall.Function.Name);
        results[toolCall.Id] = $"Tool execution failed: {ex.Message}";
      }
    }

    return results;
  }

  private void DetectAndSetActions(ShoppingChatResult result, Dictionary<string, object> toolResults)
  {
    // Check if any tool result contains checkout action marker
    foreach (var toolResult in toolResults.Values)
    {
      var resultString = toolResult?.ToString() ?? string.Empty;

      if (resultString.Contains("[CHECKOUT_ACTION]"))
      {
        // Extract URL from the result (format: "URL: http://localhost:5000")
        var urlMatch = System.Text.RegularExpressions.Regex.Match(resultString, @"URL:\s*(\S+)");
        var checkoutUrl = urlMatch.Success ? urlMatch.Groups[1].Value : "http://localhost:5000";

        result.ActionType = "redirect";
        result.ActionPayload = new Dictionary<string, string>
        {
          ["url"] = checkoutUrl,
          ["delay"] = "1500" // 1.5 seconds delay to let user read the message
        };

        // Clean up the text response to remove the marker
        result.TextResponse = result.TextResponse.Replace("[CHECKOUT_ACTION]", "").Trim();

        logger.LogInformation("Checkout action detected. Will redirect to: {Url}", checkoutUrl);
        break;
      }
    }
  }

  /// <summary>
  /// Extracts product keywords from visual context and builds a clean message
  /// that contains no image/upload references, only concrete product terms.
  /// This prevents the standalone question generator from echoing "the image" back.
  /// </summary>
  private static string ExtractCleanMessageForStandalone(string userMessage)
  {
    const string visualStart = "=== VISUAL SHOPPING CONTEXT ===";
    const string questionStart = "=== USER'S SHOPPING QUESTION ===";

    var visualIdx = userMessage.IndexOf(visualStart, StringComparison.Ordinal);
    if (visualIdx < 0)
      return userMessage;

    var questionIdx = userMessage.IndexOf(questionStart, StringComparison.Ordinal);
    if (questionIdx < 0)
      return userMessage;

    // Extract the visual block and the user's original question
    var visualBlock = userMessage.Substring(
        visualIdx + visualStart.Length,
        questionIdx - visualIdx - visualStart.Length).Trim();

    var originalQuestion = userMessage
        .Substring(questionIdx + questionStart.Length)
        .Trim();

    const string imageKeywordsPrefix =
        "Image search keywords (catalog / Azure Search — merge with the user's words in standaloneQuestion and queryString):";

    // Prefer explicit vision-derived catalog keywords (stable grounding for Azure Search + standaloneQuestion)
    string? imageSearchKeywordsLine = null;
    foreach (var line in visualBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
      var trimmed = line.Trim();
      if (trimmed.StartsWith(imageKeywordsPrefix, StringComparison.OrdinalIgnoreCase))
      {
        imageSearchKeywordsLine = trimmed.Substring(imageKeywordsPrefix.Length).Trim();
        break;
      }
    }

    if (!string.IsNullOrWhiteSpace(imageSearchKeywordsLine))
    {
      return $"""
<visual_product_grounding>
Catalog keywords from the **product** in the image only (brand, model, color, material, device type — NOT holder, hands, outdoor/indoor, or background). Mandatory: include every distinct term below in queryString; weave into standaloneQuestion as the concrete product identity. Never refer to the image, photo, or "the one shown".
{imageSearchKeywordsLine}
</visual_product_grounding>

<user_message>
{originalQuestion}
</user_message>
""";
    }

    // Parse product details from the visual context block (legacy path when keywords are absent)
    var productTerms = new List<string>();
    foreach (var line in visualBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
      var trimmed = line.Trim();

      // Extract values from structured fields
      if (trimmed.StartsWith("Visual Description:", StringComparison.OrdinalIgnoreCase))
        AddProductTerms(productTerms, trimmed.Substring("Visual Description:".Length));
      else if (trimmed.StartsWith("Product Features:", StringComparison.OrdinalIgnoreCase))
        AddProductTerms(productTerms, trimmed.Substring("Product Features:".Length));
      else if (trimmed.StartsWith("Detected Items:", StringComparison.OrdinalIgnoreCase))
        AddProductTerms(productTerms, trimmed.Substring("Detected Items:".Length));
      else if (trimmed.StartsWith("Text/Brands Visible:", StringComparison.OrdinalIgnoreCase))
        AddProductTerms(productTerms, trimmed.Substring("Text/Brands Visible:".Length));
    }

    if (productTerms.Count == 0)
      return originalQuestion;

    var productDescription = string.Join(", ", productTerms.Distinct(StringComparer.OrdinalIgnoreCase));

    // Build a clean message: "User is looking for: <product details>. <original question>"
    return $"User is looking for the following product: {productDescription}. {originalQuestion}";
  }

  private static void AddProductTerms(List<string> terms, string raw)
  {
    var value = raw.Trim();
    if (string.IsNullOrWhiteSpace(value) || value == "-" || value == "N/A")
      return;

    // Split comma-separated values and add each non-empty term
    foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
    {
      var cleaned = part.Trim();
      if (!string.IsNullOrWhiteSpace(cleaned))
        terms.Add(cleaned);
    }
  }

  private string BuildProductDataContext(ProductSelectionData? productSelectionData)
  {
    if (productSelectionData?.SelectedProducts.Any() != true)
    {
      return string.Empty;
    }

    var contextBuilder = new System.Text.StringBuilder();

    contextBuilder.AppendLine("=== User Selected Products from Table ===");
    contextBuilder.AppendLine("The user has selected the following products from the displayed table:");
    contextBuilder.AppendLine();

    foreach (var product in productSelectionData.SelectedProducts)
    {
      contextBuilder.AppendLine($"- Product: {product.ProductName}");
      contextBuilder.AppendLine($"  Product ID/URL: {product.ProductId}");
      contextBuilder.AppendLine($"  Quantity: {product.Quantity}");
      contextBuilder.AppendLine($"  Status: {(product.IsChecked ? "SELECTED (checked)" : "NOT SELECTED (unchecked)")}");

      if (product.Price.HasValue)
      {
        contextBuilder.AppendLine($"  Price: ${product.Price.Value}");
      }

      if (product.Attributes.Any())
      {
        contextBuilder.AppendLine($"  Additional Info: {string.Join(", ", product.Attributes.Select(a => $"{a.Key}={a.Value}"))}");
      }

      contextBuilder.AppendLine();
    }

    if (productSelectionData.UnselectedProducts.Any())
    {
      contextBuilder.AppendLine("Products NOT selected (unchecked):");
      contextBuilder.AppendLine(string.Join(", ", productSelectionData.UnselectedProducts));
      contextBuilder.AppendLine();
    }

    contextBuilder.AppendLine("IMPORTANT: Based on the selected products above and the user's message, call the appropriate tools:");
    contextBuilder.AppendLine("- If user wants to add to cart: call add_to_cart for each SELECTED product with their specified quantities");
    contextBuilder.AppendLine("- If user wants to remove from cart: call remove_from_cart for each UNCHECKED product");
    contextBuilder.AppendLine("- If user needs to find products: call search_products tool");
    contextBuilder.AppendLine("- Use the exact Product ID and Quantity from the selection data above");
    contextBuilder.AppendLine();
    contextBuilder.AppendLine("=== End of Selected Products ===");

    return contextBuilder.ToString();
  }
}
