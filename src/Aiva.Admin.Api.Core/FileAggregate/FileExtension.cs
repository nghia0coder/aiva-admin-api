namespace Aiva.Admin.Api.Core.FileAggregate;

/// <summary>
/// Allowed file extensions with content type mappings
/// </summary>
public static class AllowedFileExtensions
{
  public static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png",           // Images
        ".txt",                             // Text
        ".doc", ".docx",                    // Word
        ".ppt", ".pptx",                    // PowerPoint
        ".pdf",                             // PDF
        ".xls", ".xlsx"                     // Excel
    };

  public static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".txt", "text/plain" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".pdf", "application/pdf" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }
    };

  public static bool IsAllowed(string extension) => Extensions.Contains(extension);

  public static string GetContentType(string extension) =>
      ContentTypes.TryGetValue(extension, out var contentType) ? contentType : "application/octet-stream";
}
