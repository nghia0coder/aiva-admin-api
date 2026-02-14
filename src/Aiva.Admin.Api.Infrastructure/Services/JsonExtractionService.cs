using System.Text.Json;
using Aiva.Admin.Api.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class JsonExtractionService(ILogger<JsonExtractionService> logger) : IJsonExtractionService
{
    public string ExtractJson(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Input text cannot be null or empty");
            }

            logger.LogDebug("Extracting JSON from text: {Text}", text);

            // Try to find JSON wrapped in code blocks
            var jsonStart = "```json";
            var codeBlockEnd = "```";

            var jsonStartIndex = text.IndexOf(jsonStart, StringComparison.OrdinalIgnoreCase);
            if (jsonStartIndex >= 0)
            {
                // Found ```json, look for the JSON content
                var contentStart = jsonStartIndex + jsonStart.Length;
                var remainingText = text[contentStart..];

                var endIndex = remainingText.IndexOf(codeBlockEnd, StringComparison.Ordinal);
                if (endIndex >= 0)
                {
                    // Found closing ```, extract the JSON
                    var jsonContent = remainingText[..endIndex];
                    return CleanJsonString(jsonContent);
                }
                else
                {
                    // No closing ```, take everything after ```json
                    return CleanJsonString(remainingText);
                }
            }

            // Try to find JSON wrapped in generic code blocks
            var genericStart = "```";
            var genericStartIndex = text.IndexOf(genericStart, StringComparison.Ordinal);
            if (genericStartIndex >= 0)
            {
                var contentStart = genericStartIndex + genericStart.Length;
                var remainingText = text[contentStart..];

                var endIndex = remainingText.IndexOf(codeBlockEnd, StringComparison.Ordinal);
                if (endIndex >= 0)
                {
                    var jsonContent = remainingText[..endIndex];
                    return CleanJsonString(jsonContent);
                }
                else
                {
                    return CleanJsonString(remainingText);
                }
            }

            // No code blocks found, try to extract JSON directly
            // Look for opening brace
            var openBraceIndex = text.IndexOf('{');
            if (openBraceIndex >= 0)
            {
                // Find the matching closing brace
                var braceCount = 0;
                var closeBraceIndex = -1;

                for (int i = openBraceIndex; i < text.Length; i++)
                {
                    if (text[i] == '{') braceCount++;
                    else if (text[i] == '}') braceCount--;

                    if (braceCount == 0)
                    {
                        closeBraceIndex = i;
                        break;
                    }
                }

                if (closeBraceIndex >= 0)
                {
                    var jsonContent = text.Substring(openBraceIndex, closeBraceIndex - openBraceIndex + 1);
                    return CleanJsonString(jsonContent);
                }
            }

            // If no JSON structure found, return the original text cleaned
            return CleanJsonString(text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract JSON from text");
            throw new InvalidOperationException("Failed to extract JSON substring", ex);
        }
    }

    private static string CleanJsonString(string json)
    {
        return json
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ")
            .Trim();
    }
}
