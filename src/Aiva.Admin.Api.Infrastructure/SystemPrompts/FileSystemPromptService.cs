using Ardalis.Result;

namespace Aiva.Admin.Api.Infrastructure.SystemPrompts;

using Core.Interfaces;
using Core.SystemPromptAggregate;
using Microsoft.Extensions.Hosting;

/// <summary>
/// File-based system prompt service for quick testing without database.
/// Loads prompts from markdown files in the prompts/ directory.
/// </summary>
public sealed class FileSystemPromptService : ISystemPromptService
{
  private readonly ILogger<FileSystemPromptService> _logger;
  private readonly Dictionary<string, string> _promptCache = new();
  private readonly string _promptsDirectory;

  // Mapping of prompt keys to file names
  private readonly Dictionary<string, string> _promptFileMap = new()
  {
    { "default", "aiva_customer_assistant_prompt.md" },
    { "customer-support", "aiva_customer_assistant_prompt.md" },
    { "internal-assistant", "system_prompt.md" },
    { "data-assistant", "DATA_ASSISTANT_SYSTEM_PROMPT_SMARTSTORE.md" },
    { "shopping-assistant", "SHOPPING_ASSISTANT_SYSTEM_PROMPT.md" },
    { "tool-selector", "TOOL_SELECTION_PROMPT.md" },
    { "product_selection", "PRODUCT_SELECTION_EXTRACTION_PROMPT.md" }
  };

  public FileSystemPromptService(
      IHostEnvironment hostEnvironment,
      ILogger<FileSystemPromptService> logger)
  {
    _logger = logger;
    // Resolve prompts from app content root so this works in local/dev and Azure.
    _promptsDirectory = Path.Combine(hostEnvironment.ContentRootPath, "prompts");
  }

  public async Task<Result<string>> GetActivePromptContentAsync(
      SystemPromptKey key,
      CancellationToken cancellationToken = default)
  {
    try
    {
      // Check cache first
      if (_promptCache.TryGetValue(key.Value, out var cachedContent))
      {
        _logger.LogDebug(
            "Retrieved system prompt '{Key}' from memory cache",
            key.Value);
        return Result.Success(cachedContent);
      }

      // Get file name for the key
      if (!_promptFileMap.TryGetValue(key.Value, out var fileName))
      {
        _logger.LogWarning(
            "No file mapping found for prompt key '{Key}'. Using fallback.",
            key.Value);
        return GetFallbackPrompt(key);
      }

      var promptsPath = Path.Combine(_promptsDirectory, fileName);

      if (!File.Exists(promptsPath))
      {
        _logger.LogWarning(
            "Prompt file not found at '{Path}' for key '{Key}'. Using fallback.",
            promptsPath,
            key.Value);
        return GetFallbackPrompt(key);
      }

      // Read file content
      var content = await File.ReadAllTextAsync(promptsPath, cancellationToken);

      if (string.IsNullOrWhiteSpace(content))
      {
        _logger.LogWarning(
            "Prompt file '{Path}' is empty for key '{Key}'. Using fallback.",
            promptsPath,
            key.Value);
        return GetFallbackPrompt(key);
      }

      // Cache the content
      _promptCache[key.Value] = content;

      _logger.LogInformation(
          "Loaded system prompt '{Key}' from file: {Path} ({Length} characters)",
          key.Value,
          promptsPath,
          content.Length);

      return Result.Success(content);
    }
    catch (Exception ex)
    {
      _logger.LogError(
          ex,
          "Error loading system prompt '{Key}' from file",
          key.Value);
      return GetFallbackPrompt(key);
    }
  }

  public Task<Result<SystemPrompt>> GetActivePromptAsync(
      SystemPromptKey key,
      CancellationToken cancellationToken = default)
  {
    // File-based service doesn't support returning full SystemPrompt entity
    // This is primarily used for admin operations, which should use database service
    _logger.LogWarning(
        "GetActivePromptAsync called on FileSystemPromptService. " +
        "This method is not fully supported in file-based mode.");

    return Task.FromResult(
        Result<SystemPrompt>.NotFound(
            "Full SystemPrompt entity is not available in file-based mode. " +
            "Use GetActivePromptContentAsync instead."));
  }

  public void InvalidateCache(SystemPromptKey key)
  {
    if (_promptCache.Remove(key.Value))
    {
      _logger.LogInformation(
          "Invalidated cache for system prompt '{Key}'",
          key.Value);
    }
  }

  public void ClearCache()
  {
    var count = _promptCache.Count;
    _promptCache.Clear();
    _logger.LogInformation(
        "Cleared all system prompt cache ({Count} entries)",
        count);
  }

  private Result<string> GetFallbackPrompt(SystemPromptKey key)
  {
    var fallbackContent = @"You are **Aiva**, an intelligent AI assistant for customer support.

**Your Role:**
- Provide accurate, helpful information based on retrieved documents
- Always cite your sources
- Be professional, friendly, and empathetic
- Never make up information - only use data from search results

**Instructions:**
- Answer questions clearly and concisely
- If information is not found, admit it and offer to escalate
- Use retrieved context to ground your responses
- Maintain a helpful and solution-oriented tone";

    _logger.LogInformation(
        "Using fallback prompt for key '{Key}'",
        key.Value);

    return Result.Success(fallbackContent);
  }
}
