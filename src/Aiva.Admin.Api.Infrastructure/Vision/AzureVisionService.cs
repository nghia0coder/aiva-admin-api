using Azure.AI.Vision.ImageAnalysis;
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Aiva.Admin.Api.Core.Interfaces;
using Ardalis.Result;
using Aiva.Admin.Api.Infrastructure.Configuration;
using Azure;

namespace Aiva.Admin.Api.Infrastructure.Vision;

/// <summary>
/// Azure Computer Vision implementation for parsing images in shopping context
/// </summary>
public class AzureVisionService : IImageParsingService
{
    private readonly ImageAnalysisClient _client;
    private readonly ILogger<AzureVisionService> _logger;
    
    private static readonly HashSet<string> SupportedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/bmp", "image/gif", "image/webp"
    };

    public AzureVisionService(
        AppSettings appSettings,
        ILogger<AzureVisionService> logger)
    {
        _logger = logger;
        
        var endpoint = new Uri(appSettings.AzureVision.Endpoint);
        
        // Use API Key if provided, otherwise fallback to Managed Identity
        if (!string.IsNullOrWhiteSpace(appSettings.AzureVision.ApiKey))
        {
            _logger.LogDebug("Using API key authentication for Azure Computer Vision");
            var credential = new AzureKeyCredential(appSettings.AzureVision.ApiKey);
            _client = new ImageAnalysisClient(endpoint, credential);
        }
        else
        {
            _logger.LogDebug("Using managed identity authentication for Azure Computer Vision");
            var credential = new DefaultAzureCredential();
            _client = new ImageAnalysisClient(endpoint, credential);
        }
    }

    public bool IsSupported(string contentType) => 
        SupportedContentTypes.Contains(contentType);

    public async Task<Result<ImageParsingResult>> ParseImageAsync(
        System.IO.Stream imageStream,
        string fileName, 
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Parsing image {FileName} for shopping context", fileName);

            // Reset stream position if possible
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            // Convert stream to BinaryData
            var imageData = await BinaryData.FromStreamAsync(imageStream, cancellationToken);

            // Analyze image with features useful for shopping
            var result = await _client.AnalyzeAsync(
                imageData,
                VisualFeatures.Caption |           // Main description
                VisualFeatures.Tags |              // Product tags
                VisualFeatures.Objects |           // Object detection
                VisualFeatures.Read,               // OCR for brand names/text
                cancellationToken: cancellationToken);

            var analysisResult = result.Value;

            // Build shopping-focused text context
            var contextParts = new List<string>();

            // Main product description
            if (analysisResult.Caption?.Text != null)
            {
                contextParts.Add($"Product: {analysisResult.Caption.Text}");
            }

            // Extract shopping-relevant tags
            var relevantTags = ExtractShoppingTags(analysisResult.Tags?.Values);
            if (relevantTags.Any())
            {
                contextParts.Add($"Features: {string.Join(", ", relevantTags)}");
            }

            // Extract brand names or product text
            var extractedText = ExtractTextFromImage(analysisResult.Read);
            if (!string.IsNullOrWhiteSpace(extractedText))
            {
                contextParts.Add($"Text found: {extractedText}");
            }

            // Objects that could be products
            var productObjects = ExtractProductObjects(analysisResult.Objects?.Values);
            if (productObjects.Any())
            {
                contextParts.Add($"Items detected: {string.Join(", ", productObjects)}");
            }

            var combinedText = string.Join(". ", contextParts);

            _logger.LogInformation(
                "Successfully parsed image {FileName}: {WordCount} words extracted", 
                fileName, combinedText.Split(' ').Length);

            return Result.Success(new ImageParsingResult(
                ExtractedText: combinedText,
                Description: analysisResult.Caption?.Text ?? "Product image analyzed",
                Tags: relevantTags,
                Objects: productObjects));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse image {FileName}", fileName);
            return Result.Error($"Image parsing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract tags relevant to shopping/products
    /// </summary>
    private static string[] ExtractShoppingTags(IReadOnlyList<DetectedTag>? tags)
    {
        if (tags == null) return [];

        var shoppingKeywords = new[] 
        { 
            "clothing", "fashion", "handbag", "shoes", "jewelry", "watch", 
            "electronics", "furniture", "toy", "book", "accessory", "bag",
            "red", "blue", "black", "white", "brown", "green", "pink",
            "leather", "cotton", "silk", "denim", "wool", "metal", "plastic"
        };

        return tags
            .Where(tag => tag.Confidence > 0.7 && // High confidence only
                         shoppingKeywords.Any(keyword => 
                             tag.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Select(tag => tag.Name)
            .Take(10) // Limit to avoid noise
            .ToArray();
    }

    /// <summary>
    /// Extract text from image (brands, labels, etc.)
    /// </summary>
    private static string ExtractTextFromImage(ReadResult? readResult)
    {
        if (readResult?.Blocks == null) return string.Empty;

        var extractedWords = readResult.Blocks
            .SelectMany(block => block.Lines)
            .SelectMany(line => line.Words)
            .Select(word => word.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text));

        return string.Join(" ", extractedWords);
    }

    /// <summary>
    /// Extract objects that could be products
    /// </summary>
    private static string[] ExtractProductObjects(IReadOnlyList<DetectedObject>? objects)
    {
        if (objects == null) return [];

        return objects
            .Where(obj => obj.BoundingBox != null) // Ensure valid detection
            .Select(obj => obj.Tags?.FirstOrDefault()?.Name ?? "Unknown Object")
            .Where(name => !string.IsNullOrWhiteSpace(name) && name != "Unknown Object")
            .Distinct()
            .Take(5) // Limit to avoid noise
            .ToArray();
    }
}
