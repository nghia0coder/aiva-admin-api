using System.Text.Json;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class StandaloneQuestionService(ILogger<StandaloneQuestionService> logger) : IStandaloneQuestionService
{
    public StandaloneQuestionDto ParseStandaloneQuestion(string jsonContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                throw new ArgumentException("JSON string cannot be null or empty", nameof(jsonContent));
            }

            logger.LogDebug("Parsing standalone question JSON: {JsonContent}", jsonContent);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            };

            var dataStandalone = JsonSerializer.Deserialize<StandaloneQuestionDto>(jsonContent, options);

            if (dataStandalone == null)
            {
                logger.LogWarning("JSON deserialization returned null. JSON content: {JsonContent}", jsonContent);
                throw new InvalidOperationException($"Failed to deserialize JSON to StandaloneQuestionDto. JSON content: {jsonContent}");
            }

            // Log successful parsing
            logger.LogDebug("Successfully parsed standalone question. QueryString: {QueryString}, StandaloneQuestion: {StandaloneQuestion}", 
                dataStandalone.QueryString, dataStandalone.StandaloneQuestion);

            // Initialize KeyWords list and extract from QueryString
            dataStandalone.KeyWords = ExtractKeywords(dataStandalone.QueryString);

            return dataStandalone;
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "JSON deserialization failed for standalone question. Input: {JsonContent}", jsonContent);
            throw new InvalidOperationException($"Invalid JSON format: {jsonEx.Message}. Input: {jsonContent}", jsonEx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse standalone question. Input: {JsonContent}", jsonContent);
            throw new InvalidOperationException($"Failed to parse standalone question: {ex.Message}. Input: {jsonContent}", ex);
        }
    }

    private static List<string> ExtractKeywords(string? queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return new List<string>();
        }

        return queryString
            .Split(';')
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(keyword => keyword.Trim())
            .ToList();
    }
}
