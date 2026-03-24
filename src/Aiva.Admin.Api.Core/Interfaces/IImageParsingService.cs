namespace Aiva.Admin.Api.Core.Interfaces;

/// <summary>
/// Service for parsing images to extract textual context for shopping queries
/// </summary>
public interface IImageParsingService
{
    /// <summary>
    /// Parses an image and returns textual description/context for shopping
    /// </summary>
    Task<Result<ImageParsingResult>> ParseImageAsync(
        System.IO.Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the content type is supported for image parsing
    /// </summary>
    bool IsSupported(string contentType);
}

/// <summary>
/// Result of image parsing containing shopping-relevant context
/// </summary>
public record ImageParsingResult(
    string ExtractedText,
    string Description,
    string[] Tags,
    string[] Objects)
{
    public bool IsSuccess => !string.IsNullOrWhiteSpace(ExtractedText);
    public int WordCount => ExtractedText?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
}
