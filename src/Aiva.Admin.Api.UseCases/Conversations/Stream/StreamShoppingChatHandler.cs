using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Specifications;
using Aiva.Admin.Api.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public class StreamShoppingChatHandler(
    IRepository<Conversation> repository,
    IShoppingChatService shoppingChatService,
    ITitleGenerationQueueService titleGenerationQueueService,
    IImageParsingService imageParsingService,
    ILogger<StreamShoppingChatHandler> logger)
    : IRequestHandler<StreamShoppingChatCommand, Result<StreamShoppingChatResponse>>
{
  public async ValueTask<Result<StreamShoppingChatResponse>> Handle(
      StreamShoppingChatCommand request,
      CancellationToken cancellationToken)
  {
    try
    {
      // Validate and get conversation
      var conversation = await GetConversationAsync(request.ConversationId, cancellationToken);
      if (conversation == null)
      {
        return Result.NotFound("Conversation not found");
      }

      // Process images and enhance message if images are provided
      var finalMessage = await ProcessImagesAndEnhanceMessageAsync(
          request.Message, 
          request.Images, 
          cancellationToken);

      if (!finalMessage.IsSuccess)
      {
        return Result.Error($"Image processing failed: {string.Join(", ", finalMessage.Errors)}");
      }

      // Add user message to conversation
      conversation.AddMessage(ChatRole.User, finalMessage.Value);

      // Process shopping chat using enhanced message
      var shoppingResult = await shoppingChatService.ProcessShoppingChatAsync(
          conversation,
          finalMessage.Value,
          request.UserName,
          request.AdditionalUserData,
          cancellationToken);

      if (!shoppingResult.IsSuccess)
      {
        return Result.Error(string.Join("; ", shoppingResult.Errors));
      }

      // Save assistant message
      conversation.AddMessage(ChatRole.Assistant, shoppingResult.Value.TextResponse);

      // Save conversation
      await repository.UpdateAsync(conversation, cancellationToken);

      await titleGenerationQueueService.QueueTitleGenerationIfReadyAsync(conversation, cancellationToken);

      // Map to response DTO
      var response = MapToResponse(shoppingResult.Value, request.Images?.Count > 0);

      logger.LogInformation(
          "Successfully processed shopping chat for conversation {ConversationId} with {ImageCount} images", 
          request.ConversationId, request.Images?.Count ?? 0);

      return Result.Success(response);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing shopping chat stream for conversation {ConversationId}", request.ConversationId);
      return Result.Error(ex.Message);
    }
  }

  /// <summary>
  /// Process uploaded images and enhance message with visual context
  /// </summary>
  private async ValueTask<Result<string>> ProcessImagesAndEnhanceMessageAsync(
      string originalMessage,
      IReadOnlyList<ChatImageUpload>? images,
      CancellationToken cancellationToken)
  {
    // If no images, return original message
    if (images == null || images.Count == 0)
    {
      return Result.Success(originalMessage);
    }

    try
    {
      var imageContexts = new List<string>();

      // Process each image
      foreach (var image in images)
      {
        if (!IsValidImage(image))
        {
          logger.LogWarning("Skipping invalid image: {FileName}", image.FileName);
          continue;
        }

        logger.LogInformation("Processing image: {FileName}", image.FileName);

        // Parse image to extract visual context
        var parsingResult = await imageParsingService.ParseImageAsync(
            image.ImageStream,
            image.FileName,
            image.ContentType,
            cancellationToken);

        if (parsingResult.IsSuccess)
        {
          var imageContext = BuildImageContext(image.FileName, parsingResult.Value);
          imageContexts.Add(imageContext);

          logger.LogInformation("Successfully parsed image {FileName}: {WordCount} words", 
              image.FileName, parsingResult.Value.WordCount);
        }
        else
        {
          logger.LogWarning("Failed to parse image {FileName}: {Error}",
              image.FileName, string.Join(", ", parsingResult.Errors));

          // Add fallback context
          imageContexts.Add($"[Image: {image.FileName} - Could not analyze]");
        }
      }

      // Combine original message with image context
      var enhancedMessage = BuildEnhancedMessage(originalMessage, imageContexts);

      return Result.Success(enhancedMessage);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing images for chat message");
      return Result.Error($"Image processing failed: {ex.Message}");
    }
  }

  /// <summary>
  /// Build enhanced message combining text with image contexts
  /// </summary>
  private static string BuildEnhancedMessage(string originalMessage, List<string> imageContexts)
  {
    if (imageContexts.Count == 0)
    {
      return originalMessage;
    }

    var messageBuilder = new System.Text.StringBuilder();

    // Add visual shopping context with clear instructions
    messageBuilder.AppendLine("=== VISUAL SHOPPING CONTEXT ===");
    messageBuilder.AppendLine("The user has uploaded images to assist with their shopping inquiry. Please analyze the visual content to provide relevant product recommendations, comparisons, or shopping assistance.");

    // Special instruction for cart-related queries with images
    if (originalMessage.Contains("cart", StringComparison.OrdinalIgnoreCase) || 
        originalMessage.Contains("giỏ", StringComparison.OrdinalIgnoreCase))
    {
      messageBuilder.AppendLine();
      messageBuilder.AppendLine("CART + VISUAL CONTEXT GUIDANCE:");
      messageBuilder.AppendLine("- If this is a cart inquiry with images, the user may want to compare cart items with uploaded product images");
      messageBuilder.AppendLine("- Use visual context to suggest similar items, alternatives, or complementary products");
      messageBuilder.AppendLine("- When displaying cart information, format as a table with Cart ID for easy management");
      messageBuilder.AppendLine("- Reference visual similarities between cart items and uploaded images");
    }

    messageBuilder.AppendLine();

    foreach (var context in imageContexts)
    {
      messageBuilder.AppendLine(context);
      messageBuilder.AppendLine(); // Add space between images
    }

    // Add clear separation and user's question
    messageBuilder.AppendLine("=== USER'S SHOPPING QUESTION ===");
    messageBuilder.AppendLine(originalMessage);

    return messageBuilder.ToString();
  }

  /// <summary>
  /// Build structured context from image parsing result
  /// </summary>
  private static string BuildImageContext(string fileName, ImageParsingResult result)
  {
    var context = new List<string>
    {
      $"**Image: {fileName}**",
      $"Visual Description: {result.Description}"
    };

    // Enhanced shopping context formatting
    if (result.Tags?.Length > 0)
    {
      context.Add($"Product Features: {string.Join(", ", result.Tags)}");
    }

    if (result.Objects?.Length > 0)
    {
      context.Add($"Detected Items: {string.Join(", ", result.Objects)}");
    }

    if (!string.IsNullOrWhiteSpace(result.ExtractedText))
    {
      context.Add($"Text/Brands Visible: {result.ExtractedText}");
    }

    // Add shopping intent guidance
    context.Add("Shopping Context: This image shows products or items the user wants to inquire about, purchase, or get recommendations for.");

    return string.Join("\n", context);
  }

  /// <summary>
  /// Validate uploaded image for shopping context
  /// </summary>
  private bool IsValidImage(ChatImageUpload image)
  {
    // Check if parsing service supports the content type
    if (!imageParsingService.IsSupported(image.ContentType))
    {
      logger.LogWarning("Unsupported content type for image {FileName}: {ContentType}", 
          image.FileName, image.ContentType);
      return false;
    }

    // Check file size (max 10MB for shopping images)
    const long maxSizeBytes = 10 * 1024 * 1024;
    if (image.FileSizeBytes > maxSizeBytes)
    {
      logger.LogWarning("Image {FileName} too large for shopping analysis: {Size} bytes (max: {MaxSize})", 
          image.FileName, image.FileSizeBytes, maxSizeBytes);
      return false;
    }

    // Check stream availability
    if (image.ImageStream == null || !image.ImageStream.CanRead)
    {
      logger.LogWarning("Invalid or unreadable stream for image {FileName}", image.FileName);
      return false;
    }

    // Validate minimum size for meaningful shopping analysis (at least 10KB)
    const long minSizeBytes = 10 * 1024;
    if (image.FileSizeBytes < minSizeBytes)
    {
      logger.LogWarning("Image {FileName} too small for reliable shopping analysis: {Size} bytes", 
          image.FileName, image.FileSizeBytes);
      return false;
    }

    return true;
  }

  private async Task<Conversation?> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken)
  {
    var spec = new ConversationByIdWithMessagesSpec(ConversationId.From(conversationId));
    return await repository.FirstOrDefaultAsync(spec, cancellationToken);
  }

  private static StreamShoppingChatResponse MapToResponse(ShoppingChatResult result, bool hadImages = false)
  {
    return new StreamShoppingChatResponse
    {
      TextResponse = result.TextResponse,
      HasProducts = result.HasProducts,
      ToolsExecuted = result.ToolsExecuted,
      ActionType = result.ActionType,
      ActionPayload = result.ActionPayload,
      ProcessedImages = hadImages
    };
  }

}
