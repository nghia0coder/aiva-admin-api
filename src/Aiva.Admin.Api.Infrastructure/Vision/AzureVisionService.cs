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

            var captionText = analysisResult.Caption?.Text ?? string.Empty;
            var ocrPlain = ExtractTextFromImage(analysisResult.Read);
            var searchKeywords = BuildSearchKeywords(
                captionText,
                analysisResult.Tags?.Values,
                analysisResult.Objects?.Values,
                ocrPlain);
            if (searchKeywords.Length == 0 && !string.IsNullOrWhiteSpace(captionText))
            {
                var captionTokens = TokenizeCaptionForKeywords(captionText).Take(12).ToArray();
                if (captionTokens.Length > 0)
                    searchKeywords = captionTokens;
            }

            var combinedText = string.Join(". ", contextParts);

            _logger.LogInformation(
                "Successfully parsed image {FileName}: {WordCount} words extracted", 
                fileName, combinedText.Split(' ').Length);

            return Result.Success(new ImageParsingResult(
                ExtractedText: combinedText,
                Description: string.IsNullOrWhiteSpace(captionText) ? "Product image analyzed" : captionText,
                Tags: relevantTags,
                Objects: productObjects,
                SearchKeywords: searchKeywords));
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
            "phone", "smartphone", "mobile", "cell", "tablet", "laptop", "computer",
            "headphone", "earbud", "camera", "television", "monitor", "keyboard",
            "red", "blue", "black", "white", "brown", "green", "pink",
            "leather", "cotton", "silk", "denim", "wool", "metal", "plastic"
        };

        return tags
            .Where(tag => tag.Confidence > 0.7 && // High confidence only
                         !IsSceneOrHolderVisionTag(tag.Name) &&
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
            .Where(name => !IsSceneOrHolderObjectName(name))
            .Distinct()
            .Take(5) // Limit to avoid noise
            .ToArray();
    }

    /// <summary>
    /// Ordered, deduplicated terms for catalog search: product/OCR first; scene, holder, and environment terms excluded.
    /// </summary>
    private static string[] BuildSearchKeywords(
        string? caption,
        IReadOnlyList<DetectedTag>? tags,
        IReadOnlyList<DetectedObject>? objects,
        string ocrText)
    {
        var ordered = new List<string>();

        void Add(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return;
            var t = s.Trim();
            if (t.Length < 2) return;
            if (IsSceneOrHolderKeywordPhrase(t)) return;
            if (ordered.Any(x => x.Equals(t, StringComparison.OrdinalIgnoreCase))) return;
            ordered.Add(t);
        }

        // 1) Detected product-like objects (e.g. mobile phone), not people/hands/trees
        foreach (var obj in objects ?? [])
        {
            var name = obj.Tags?.FirstOrDefault()?.Name;
            Add(name);
        }

        // 2) OCR — brands, model names on device/box (highest signal for exact product match)
        foreach (var token in TokenizeCaptionForKeywords(ocrText))
            Add(token);

        // 3) Vision tags — drop outdoor/hand/person/nature etc.
        foreach (var tag in (tags ?? []).OrderByDescending(t => t.Confidence))
        {
            if (tag.Confidence < 0.5 || string.IsNullOrWhiteSpace(tag.Name)) continue;
            if (IsSceneOrHolderVisionTag(tag.Name)) continue;
            Add(tag.Name);
        }

        // 4) Caption tokens last (captions often describe the scene; keep only after product signals)
        foreach (var token in TokenizeCaptionForKeywords(caption))
            Add(token);

        return ordered.Take(25).ToArray();
    }

    /// <summary>Vision tags that describe photo context, not merchandise (catalog search noise).</summary>
    private static bool IsSceneOrHolderVisionTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return true;
        var words = SplitTagWords(tagName).ToList();
        if (words.Count == 0) return true;
        // One-word scene tags (e.g. "outdoor", "person") — not product descriptors
        if (words.Count == 1 && SceneHolderTagWords.Contains(words[0])) return true;
        // Phrase is only scene/holder vocabulary (e.g. "green leaves", "blurred background")
        if (words.All(w => SceneHolderTagWords.Contains(w))) return true;
        return false;
    }

    private static bool IsSceneOrHolderObjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var lower = name.Trim().ToLowerInvariant();
        if (NonProductDetectedObjectNames.Contains(lower)) return true;
        // Azure sometimes returns "Person with ..." style labels (avoid matching "personal", "personnel")
        if (lower == "person" || lower.StartsWith("person ", StringComparison.Ordinal) ||
            lower.StartsWith("person-", StringComparison.Ordinal))
            return true;
        return false;
    }

    /// <summary>Object detection classes that are scene/human, not catalog SKUs.</summary>
    private static readonly HashSet<string> NonProductDetectedObjectNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "person", "people", "human", "man", "woman", "boy", "girl", "child", "baby", "adult",
        "face", "head", "hair", "hand", "hands", "finger", "fingers", "arm", "arms", "leg", "legs", "foot", "feet",
        "tree", "trees", "plant", "plants", "flower", "flowers", "grass", "sky", "cloud", "animal", "dog", "cat", "bird",
        "bicycle", "motorcycle", "car", "bus", "truck", "vehicle", "wheel", "building", "house", "road", "water"
    };

    /// <summary>Keyword phrase is scene/holder only — drop from catalog list.</summary>
    private static bool IsSceneOrHolderKeywordPhrase(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return true;
        var words = SplitTagWords(phrase).ToList();
        if (words.Count == 0) return true;
        if (words.Count == 1 && SceneHolderTagWords.Contains(words[0])) return true;
        return words.All(w => SceneHolderTagWords.Contains(w) || w.Length < 3);
    }

    private static IEnumerable<string> SplitTagWords(string text) =>
        text.Split(new[] { ' ', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length > 0);

    private static readonly HashSet<string> SceneHolderTagWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "outdoor", "outdoors", "indoor", "indoors", "nature", "foliage", "forest", "jungle", "beach",
        "sky", "cloud", "clouds", "sunlight", "sunny", "grass", "tree", "trees", "leaf", "leaves",
        "plant", "plants", "mountain", "lake", "river", "ocean", "water", "park", "garden", "field",
        "background", "bokeh", "blurred", "blur", "person", "people", "human", "man", "woman", "boy",
        "girl", "child", "baby", "face", "hand", "hands", "finger", "fingers", "thumb", "arm", "arms",
        "walking", "standing", "sitting", "holding", "smile", "selfie", "portrait", "hiking", "road",
        "sidewalk", "street", "vehicle", "car", "bicycle", "dog", "cat", "bird"
    };

    private static IEnumerable<string> TokenizeCaptionForKeywords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could", "should",
            "may", "might", "must", "can", "need", "with", "from", "for", "to", "of", "in", "on",
            "at", "by", "as", "and", "or", "but", "if", "it", "its", "this", "that",
            "these", "those", "there", "here", "some", "any", "very", "just", "into",
            "holding", "held", "showing", "shown", "standing", "sitting", "wearing", "using", "looking",
            "person", "people", "man", "woman", "boy", "girl", "child", "baby", "hand", "hands",
            "finger", "fingers", "thumb", "arm", "background", "image", "photo", "picture", "close", "up",
            "outdoor", "outdoors", "indoor", "indoors", "outside", "inside", "handheld", "bright", "sunny",
            "sunlight", "blurred", "blur", "bokeh", "foliage", "leaves", "leaf", "tree", "trees", "grass",
            "nature", "sky", "park", "forest", "beach", "walking", "vertical", "horizontal"
        };

        foreach (var raw in text.Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '!', '?' },
                     StringSplitOptions.RemoveEmptyEntries))
        {
            var token = new string(raw.Where(char.IsLetterOrDigit).ToArray());
            if (token.Length < 3 || stop.Contains(token)) continue;
            if (SceneHolderTagWords.Contains(token)) continue;
            yield return token;
        }
    }
}
