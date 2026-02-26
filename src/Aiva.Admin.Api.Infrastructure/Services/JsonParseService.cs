using System.Text.Json;
using Aiva.Admin.Api.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class JsonParseService(ILogger<JsonParseService> logger) : IJsonParseService
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public T Parse<T>(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            throw new ArgumentException("JSON string cannot be null or empty", nameof(jsonContent));
        }

        logger.LogDebug("Parsing JSON to {Type}: {JsonContent}", typeof(T).Name, jsonContent);

        try
        {
            var result = JsonSerializer.Deserialize<T>(jsonContent, DefaultOptions);

            if (result is null)
            {
                logger.LogWarning("JSON deserialization returned null. Content: {JsonContent}", jsonContent);
                throw new InvalidOperationException(
                    $"Failed to deserialize JSON to {typeof(T).Name}. Content: {jsonContent}");
            }

            // ReSharper disable once TemplateIsNotCompileTimeConstant
            logger.LogDebug("Successfully parsed JSON to {Type}", typeof(T).Name);
            return result;
        }
        catch (JsonException jsonEx)
        {
            logger.LogError(jsonEx, "JSON deserialization failed. Input: {JsonContent}", jsonContent);
            throw new InvalidOperationException(
                $"Invalid JSON format: {jsonEx.Message}. Input: {jsonContent}", jsonEx);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            logger.LogError(ex, "Failed to parse JSON. Input: {JsonContent}", jsonContent);
            throw new InvalidOperationException(
                $"Failed to parse JSON: {ex.Message}. Input: {jsonContent}", ex);
        }
    }
}
