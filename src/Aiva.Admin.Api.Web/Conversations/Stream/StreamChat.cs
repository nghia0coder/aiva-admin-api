using System.Text.Json;
using Aiva.Admin.Api.Core.UserAggregate;
using Aiva.Admin.Api.UseCases.Conversations.Stream;
using Aiva.Admin.Api.Web.Common;
using Aiva.Admin.Api.Web.Services.Streaming;
using Ardalis.SharedKernel;

namespace Aiva.Admin.Api.Web.Conversations.Stream;

/// <summary>
/// Streaming chat endpoint with optional file upload support.
/// 
/// Supports two content types:
/// 1. application/json - For text-only messages
/// 2. multipart/form-data - For messages with image uploads
/// 
/// The endpoint automatically detects the content type and handles files accordingly.
/// </summary>
public class StreamChat(
    IMediator mediator,
    IStreamingService streamingService,
    IRepository<User> userRepository,
    ILogger<StreamChat> logger)
    : AuthenticatedEndpoint<StreamChatRequest, object>
{
  public override void Configure()
  {
    Post(StreamChatRequest.Route);
    // Remove AllowFileUploads() to handle both JSON and multipart requests
    AllowFormData(); // This allows both JSON and multipart/form-data
    Summary(s =>
    {
      s.Summary = "Stream AI response with Intent Routing and Image Support";
      s.Description = """
        Enterprise-grade streaming with SQL/RAG routing based on intent and image processing capabilities.

        Features:
        - Text message processing (JSON or form-data)
        - Image upload and analysis (up to 5 images, max 10MB each) via multipart/form-data
        - Intent-based routing (Data Assistant for Admins, Shopping Assistant for Customers)
        - Real-time streaming responses
        - Visual shopping context integration

        Supported image types: JPEG, PNG, BMP, GIF, WebP
        Content-Type: application/json (text only) or multipart/form-data (with images)
        """;
      s.RequestParam(r => r.ConversationId, "Conversation identifier");
      s.RequestParam(r => r.Message, "User's text message");
      s.RequestParam(r => r.AdditionalUserData, "Additional context data (optional)");
      s.RequestParam(r => r.HasImages, "Indicates if images are included (optional)");
    });
    Tags("Conversations");
  }

  public override async Task HandleAsync(StreamChatRequest request, CancellationToken ct)
  {
    try
    {
      // Check authentication
      if (!IsAuthenticated)
      {
        await streamingService.SendEventAsync(HttpContext, "error", new { message = "User not authenticated" }, ct);
        return;
      }

      // Get current user to determine role
      var currentUser = await userRepository.GetByIdAsync(RequiredUserId, ct);
      if (currentUser == null)
      {
        await streamingService.SendEventAsync(HttpContext, "error", new { message = "User not found" }, ct);
        return;
      }

      streamingService.ConfigureResponse(HttpContext);

      // Log request content type for debugging
      logger.LogDebug("Request Content-Type: {ContentType}", HttpContext.Request.ContentType);

      // Route based on user role
      if (currentUser.Role == UserRole.Admin)
      {
        // Handle Admin flow (DataAssistant) - existing logic
        await HandleDataAssistantFlow(request, ct);
      }
      else if (currentUser.Role == UserRole.Customer)
      {
        // Handle Customer flow (ShoppingAssistant)
        await HandleShoppingAssistantFlow(request, FullName ?? "User", ct);
      }
      else
      {
        await streamingService.SendEventAsync(HttpContext, "error", new { message = "Invalid user role" }, ct);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error in stream chat endpoint");
      await streamingService.SendEventAsync(HttpContext, "error", new { message = ex.Message }, ct);
    }
  }

  private async Task HandleDataAssistantFlow(StreamChatRequest request, CancellationToken ct)
  {
    var command = new StreamDataChatCommand(request.ConversationId, request.Message);
    var result = await mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      var response = result.Value;

      // Stream text response
      await streamingService.SendEventAsync(HttpContext, "message", new
      {
        content = response.TextResponse,
        type = "text"
      }, ct);

      // Stream markdown table if available
      if (!string.IsNullOrEmpty(response.MarkdownTable))
      {
        await streamingService.SendEventAsync(HttpContext, "table", new
        {
          content = response.MarkdownTable,
          type = "markdown"
        }, ct);
      }

      // Stream chart URL if available
      if (response.HasChart && !string.IsNullOrEmpty(response.ChartUrl))
      {
        await streamingService.SendEventAsync(HttpContext, "chart", new
        {
          url = response.ChartUrl,
          type = "highchart"
        }, ct);
      }

      // Stream chart config if available (Chart.js)
      if (response.HasChart && !string.IsNullOrEmpty(response.ChartConfig))
      {
        try
        {
          var chartConfigJson = JsonDocument.Parse(response.ChartConfig).RootElement;

          await streamingService.SendEventAsync(HttpContext, "chart", new
          {
            config = chartConfigJson,
            chartType = response.ChartType,
            type = "chartjs"
          }, ct);
        }
        catch (JsonException ex)
        {
          logger.LogError(ex, "Failed to parse chart config JSON");
          // Fallback: send as raw string
          await streamingService.SendEventAsync(HttpContext, "chart", new
          {
            config = response.ChartConfig,
            chartType = response.ChartType,
            type = "chartjs"
          }, ct);
        }
      }

      await streamingService.SendEventAsync(HttpContext, "done", new { complete = true }, ct);
    }
    else
    {
      await streamingService.SendEventAsync(HttpContext, "error", new { message = result.ValidationErrors?.FirstOrDefault() }, ct);
    }
  }

  private async Task HandleShoppingAssistantFlow(StreamChatRequest request, string userName, CancellationToken ct)
  {
    // Process uploaded images if any (only for multipart/form-data requests)
    var images = new List<ChatImageUpload>();

    // Check if this is a multipart request with files
    var isMultipartRequest = HttpContext.Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true;

    if (isMultipartRequest && Files.Any())
    {
      logger.LogInformation("Processing multipart request with {FileCount} files", Files.Count());

      // Validate image count
      if (Files.Count() > StreamChatRequest.MaxImagesAllowed)
      {
        await streamingService.SendEventAsync(HttpContext, "error", new 
        { 
          message = $"Too many images. Maximum allowed: {StreamChatRequest.MaxImagesAllowed}" 
        }, ct);
        return;
      }

      foreach (var file in Files)
      {
        // Validate file size
        if (file.Length > StreamChatRequest.MaxImageSizeBytes)
        {
          await streamingService.SendEventAsync(HttpContext, "error", new 
          { 
            message = $"Image '{file.Name}' is too large. Maximum size: {StreamChatRequest.MaxImageSizeBytes / 1024 / 1024}MB" 
          }, ct);
          return;
        }

        if (IsValidImageFile(file.ContentType))
        {
          logger.LogInformation("Processing uploaded image: {FileName} ({ContentType}, {Size} bytes)", 
              file.Name, file.ContentType, file.Length);

          var imageUpload = new ChatImageUpload(
              FileName: file.Name,
              ContentType: file.ContentType,
              ImageStream: file.OpenReadStream(),
              FileSizeBytes: file.Length);

          images.Add(imageUpload);
        }
        else
        {
          logger.LogWarning("Skipping unsupported file type: {FileName} ({ContentType})", 
              file.Name, file.ContentType);
        }
      }
    }
    else if (isMultipartRequest)
    {
      logger.LogInformation("Received multipart request with no files");
    }
    else
    {
      logger.LogInformation("Received JSON request (no file upload capability)");
    }

    var command = new StreamShoppingChatCommand(
        request.ConversationId, 
        userName, 
        request.Message,
        request.AdditionalUserData,
        images.AsReadOnly());
    var result = await mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      var response = result.Value;

      // Stream text response
      await streamingService.SendEventAsync(HttpContext, "message", new
      {
        content = response.TextResponse,
        type = "text",
        processedImages = response.ProcessedImages,
        imageCount = images.Count
      }, ct);

      // Stream action if present (e.g., redirect to checkout)
      if (!string.IsNullOrEmpty(response.ActionType) && response.ActionPayload != null)
      {
        await streamingService.SendEventAsync(HttpContext, "action", new
        {
          actionType = response.ActionType,
          payload = response.ActionPayload
        }, ct);
      }

      await streamingService.SendEventAsync(HttpContext, "done", new { complete = true }, ct);
    }
    else
    {
      await streamingService.SendEventAsync(HttpContext, "error", new { message = result.ValidationErrors?.FirstOrDefault() }, ct);
    }
  }

  /// <summary>
  /// Validate if uploaded file is a supported image type
  /// </summary>
  private static bool IsValidImageFile(string contentType)
  {
    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/bmp", "image/gif", "image/webp" };
    return allowedTypes.Contains(contentType.ToLowerInvariant());
  }
}
