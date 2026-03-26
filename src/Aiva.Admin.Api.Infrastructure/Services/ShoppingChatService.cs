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

        // Retrieval tools (search_infors, get_product_info) return catalog data that the shopping
        // assistant must render as an interactive product table using <shopping_cart_rules>.
        // Action tools (add_to_cart, remove_from_cart, update_cart, clear_cart, checkout, get_cart)
        // have already mutated state and only need a brief confirmation.
        var isRetrievalOnly = toolSelectionResult.Value.All(tc => IsRetrievalTool(tc.Function.Name));

        string systemPrompt;
        string finalUserMessage;

        if (isRetrievalOnly)
        {
          // Let the shopping assistant display the product table normally
          systemPrompt = promptTemplateService.ReplacePromptByKey(
            systemPromptTemplate,
            new ReplacePromptDto
            {
              FullName = !string.IsNullOrWhiteSpace(userName) ? userName : "User",
              ProductData = string.Join("\n", toolResults.Values),
              ToolResultsInstruction = string.Empty
            });
          finalUserMessage = dataStandalone.StandaloneQuestion;
        }
        else
        {
          // Action tools: tell the AI the action is done and to confirm it
          systemPrompt = promptTemplateService.ReplacePromptByKey(
            systemPromptTemplate,
            new ReplacePromptDto
            {
              FullName = !string.IsNullOrWhiteSpace(userName) ? userName : "User",
              ProductData = string.Join("\n", toolResults.Values),
              ToolResultsInstruction = BuildToolResultsInstruction()
            });
          finalUserMessage = BuildToolExecutionCompletedMessage(
              dataStandalone.StandaloneQuestion,
              toolSelectionResult.Value,
              toolResults);
        }

        var finalResponse = await chatService.GetCompletionAsync(systemPrompt, finalUserMessage);

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

  private static bool IsRetrievalTool(string toolName) =>
      toolName is "search_infors" or "get_product_info";

  private static string BuildToolExecutionCompletedMessage(
      string originalQuestion,
      List<ToolCall> toolCalls,
      Dictionary<string, object> toolResults)
  {
    var sb = new System.Text.StringBuilder();
    sb.AppendLine("[TOOL EXECUTION ALREADY COMPLETED — DO NOT ASK THE USER FOR MORE INFORMATION]");
    sb.AppendLine();
    sb.AppendLine($"User's original request: {originalQuestion}");
    sb.AppendLine();
    sb.AppendLine("Tools executed and their results:");
    foreach (var toolCall in toolCalls)
    {
      if (toolResults.TryGetValue(toolCall.Id, out var result))
      {
        sb.AppendLine($"• {toolCall.Function.Name}: {result}");
      }
    }
    sb.AppendLine();
    sb.AppendLine("The action(s) above have already been carried out. Confirm the completed action(s) to the user concisely. Do NOT ask for information that was already provided or request the user to repeat an action that succeeded.");
    return sb.ToString();
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

MANDATORY INTERACTIVE ELEMENTS FOR CART TABLE (ALWAYS REQUIRED):
- Selection Column (ALWAYS REQUIRED): EVERY row MUST have a real checkbox element that users can click to select/deselect items
  * Format: `<input type="checkbox" value="{cartId}" checked="checked">`
  * The checkbox MUST be an actual `<input type="checkbox">` element, NOT plain text, NOT an emoji, NOT a symbol
  * ALWAYS set checked="checked" by default so users can deselect if needed
  * The value attribute MUST contain the Cart ID for that row
  
- Quantity Controls (ALWAYS REQUIRED): EVERY row MUST have an editable number input field that users can directly type into or use browser increment/decrement arrows
  * Format: `<input type="number" min="1" value="{quantity}" data-cart-id="{cartId}" style="width: 60px; padding: 4px; text-align: center;">`
  * The quantity MUST be an actual `<input type="number">` element, NOT plain text, NOT emoji arrows like "🔺 3 🔻", NOT buttons like "[+] 3 [-]"
  * Users must be able to directly click and type a new quantity value
  * The data-cart-id attribute MUST contain the Cart ID for that row
  
- Action Buttons (ALWAYS REQUIRED): EVERY row MUST have a real remove button element
  * Format: `<button type="button" onclick="removeCartItem({cartId})">🗑️ Remove</button>`
  * OR: `<button type="button" data-cart-id="{cartId}" style="background: #dc3545; color: white; border: none; padding: 4px 8px; border-radius: 3px; cursor: pointer;">Remove</button>`
  
- Bulk Actions (ALWAYS REQUIRED): At table bottom, show options like "Remove Selected" and "Update Quantities"

CRITICAL RULES - NO EXCEPTIONS:
1. NEVER use plain text for checkboxes (❌ "☑" or "✓" or "[x]")
2. NEVER use plain text or emojis for quantity (❌ "🔺 3 🔻" or "[+] 3 [-]")
3. ALWAYS use actual HTML input elements (`<input type="checkbox">` and `<input type="number">`)
4. EVERY cart item row MUST be fully interactive with selectable checkbox and editable quantity
5. If you cannot create interactive elements, do NOT display the cart table at all

EXAMPLE CART TABLE FORMAT (HTML) - FOLLOW THIS EXACTLY:
```html
<table border="1" cellspacing="0" cellpadding="8" style="border-collapse: collapse; width: 100%; font-family: Arial, sans-serif;">
  <thead style="background-color: #f8f9fa;">
    <tr>
      <th width="50">Select</th>
      <th width="80">Cart ID</th>
      <th>Product</th>
      <th width="100">SKU</th>
      <th width="100">Quantity</th>
      <th width="100">Unit Price</th>
      <th>Attributes</th>
      <th width="100">Subtotal</th>
      <th width="80">Actions</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td style="text-align: center;">
        <input type="checkbox" value="123" checked="checked">
      </td>
      <td style="text-align: center;">123</td>
      <td>iPhone 15 Pro (ID: 15)</td>
      <td>IP15P-128</td>
      <td style="text-align: center;">
        <input type="number" min="1" value="2" data-cart-id="123" style="width: 60px; padding: 4px; text-align: center; border: 1px solid #ccc; border-radius: 3px;">
      </td>
      <td>$799.00</td>
      <td>128GB, Blue</td>
      <td>$1,598.00</td>
      <td style="text-align: center;">
        <button type="button" data-cart-id="123" style="background: #dc3545; color: white; border: none; padding: 4px 8px; border-radius: 3px; cursor: pointer;">Remove</button>
      </td>
    </tr>
    <tr>
      <td style="text-align: center;">
        <input type="checkbox" value="124" checked="checked">
      </td>
      <td style="text-align: center;">124</td>
      <td>AirPods Pro (ID: 42)</td>
      <td>APP-2023</td>
      <td style="text-align: center;">
        <input type="number" min="1" value="1" data-cart-id="124" style="width: 60px; padding: 4px; text-align: center; border: 1px solid #ccc; border-radius: 3px;">
      </td>
      <td>$249.00</td>
      <td>White</td>
      <td>$249.00</td>
      <td style="text-align: center;">
        <button type="button" data-cart-id="124" style="background: #dc3545; color: white; border: none; padding: 4px 8px; border-radius: 3px; cursor: pointer;">Remove</button>
      </td>
    </tr>
  </tbody>
</table>

<div style="margin-top: 16px; padding: 12px; background-color: #f8f9fa; border-radius: 4px;">
  <strong>Cart Summary:</strong><br>
  Total Items: 3<br>
  Subtotal: $1,847.00<br>
  <button type="button" style="margin-top: 8px; background: #007bff; color: white; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer;">Proceed to Checkout</button>
</div>
```

USER INSTRUCTIONS TO INCLUDE WITH CART TABLE:
- Include the cart summary below the table with totals and a checkout button
- ALWAYS explain to users that they can:
  * ✅ **Select/deselect items** by clicking the checkboxes in the "Select" column
  * ✅ **Adjust quantities** by clicking on the number field and typing a new value (or using browser up/down arrows)
  * ✅ **Remove items** by clicking the "Remove" button for that item
  * ✅ **Use Cart ID** for precise remove operations in voice commands (e.g., "remove cart item 123")
  * ✅ **Bulk operations**: Select multiple items and use bulk actions

CRITICAL VALIDATION CHECKLIST (verify EVERY time you generate a cart table):
- [ ] Does EVERY row have `<input type="checkbox" value="{cartId}" checked="checked">`?
- [ ] Does EVERY row have `<input type="number" min="1" value="{quantity}" data-cart-id="{cartId}">`?
- [ ] Does EVERY row have a Remove button with data-cart-id attribute?
- [ ] Are Cart IDs displayed in a dedicated column?
- [ ] Is the table properly formatted with borders and padding?
- [ ] Are there NO plain text checkboxes (❌ "☑", "✓", "[x]")?
- [ ] Are there NO emoji quantity controls (❌ "🔺 3 🔻")?

If you cannot verify ALL items in the checklist, DO NOT generate the cart table.

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
