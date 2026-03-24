namespace Aiva.Admin.Api.Web.Conversations.Stream;

/// <summary>
/// Request model for streaming chat with support for text messages and image uploads
/// </summary>
public class StreamChatRequest
{
  public const string Route = "/conversations/{ConversationId}/stream";

  /// <summary>
  /// The conversation identifier
  /// </summary>
  public Guid ConversationId { get; set; }

  /// <summary>
  /// The user's text message
  /// </summary>
  public string Message { get; set; } = string.Empty;

  /// <summary>
  /// Additional user context data (e.g., cart contents, user preferences)
  /// </summary>
  public string AdditionalUserData { get; set; } = string.Empty;

  /// <summary>
  /// Indicates if images are being uploaded with this request
  /// Note: Actual image files are handled through FastEndpoints' file upload mechanism
  /// </summary>
  public bool HasImages { get; set; } = false;

  /// <summary>
  /// Maximum number of images allowed per request
  /// </summary>
  public const int MaxImagesAllowed = 5;

  /// <summary>
  /// Maximum file size per image (10MB)
  /// </summary>
  public const long MaxImageSizeBytes = 10 * 1024 * 1024;
}
