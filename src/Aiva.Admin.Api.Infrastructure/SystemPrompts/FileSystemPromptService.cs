using Ardalis.Result;

namespace Aiva.Admin.Api.Infrastructure.SystemPrompts;

using Core.Interfaces;
using Core.SystemPromptAggregate;
using Microsoft.Extensions.Hosting;

/// <summary>
/// File-based system prompt service for quick testing without database.
/// Loads prompts from markdown files in the prompts/ directory.
/// Supports multiple path resolution strategies for local and Azure deployment.
/// </summary>
public sealed class FileSystemPromptService : ISystemPromptService
{
  private readonly ILogger<FileSystemPromptService> _logger;
  private readonly Dictionary<string, string> _promptCache = new();
  private readonly List<string> _promptsDirectories;

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

    // Initialize multiple fallback paths for different deployment scenarios
    _promptsDirectories = InitializePromptDirectories(hostEnvironment);

    // Log all attempted paths for debugging
    _logger.LogInformation("FileSystemPromptService initialized with {PathCount} search paths:", _promptsDirectories.Count);
    for (int i = 0; i < _promptsDirectories.Count; i++)
    {
      var exists = Directory.Exists(_promptsDirectories[i]);
      _logger.LogInformation("  {Index}. {Path} (exists: {Exists})", i + 1, _promptsDirectories[i], exists);
    }
  }

  private List<string> InitializePromptDirectories(IHostEnvironment hostEnvironment)
  {
    var directories = new List<string>();

    // 1. Content root path (standard for ASP.NET Core)
    var contentRootPromptsPath = Path.Combine(hostEnvironment.ContentRootPath, "prompts");
    directories.Add(contentRootPromptsPath);

    // 2. App domain base directory (works in many deployment scenarios)
    var appDomainPromptsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompts");
    directories.Add(appDomainPromptsPath);

    // 3. Relative to current working directory (Azure App Service)
    var workingDirectoryPromptsPath = Path.Combine(Directory.GetCurrentDirectory(), "prompts");
    directories.Add(workingDirectoryPromptsPath);

    // 4. Azure specific paths - App Service deploys to /home/site/wwwroot
    if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null)
    {
      // Azure App Service specific paths
      var azureWwwRootPath = Path.Combine("/home/site/wwwroot", "prompts");
      directories.Add(azureWwwRootPath);

      var azureAppPath = Path.Combine("/home/site/wwwroot/app", "prompts");
      directories.Add(azureAppPath);
    }

    // 5. Solution root fallback (for local development)
    try
    {
      var solutionRootPath = GetSolutionRootPath(hostEnvironment.ContentRootPath);
      if (!string.IsNullOrEmpty(solutionRootPath))
      {
        var solutionPromptsPath = Path.Combine(solutionRootPath, "prompts");
        directories.Add(solutionPromptsPath);
      }
    }
    catch (Exception ex)
    {
      _logger.LogDebug(ex, "Could not determine solution root path");
    }

    // 6. Environment variable override
    var envPromptsPath = Environment.GetEnvironmentVariable("PROMPTS_DIRECTORY");
    if (!string.IsNullOrEmpty(envPromptsPath))
    {
      directories.Add(envPromptsPath);
    }

    // Remove duplicates while preserving order
    return directories.Distinct().ToList();
  }

  private static string? GetSolutionRootPath(string contentRootPath)
  {
    var current = new DirectoryInfo(contentRootPath);

    // Look for common solution indicators going up the directory tree
    while (current != null)
    {
      // Look for .sln files or typical solution structure
      if (current.GetFiles("*.sln").Any() || 
          current.GetDirectories("src").Any() ||
          current.GetFiles("Directory.Packages.props").Any())
      {
        return current.FullName;
      }
      current = current.Parent;
    }

    return null;
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

      // Try to find the file in any of the search directories
      string? foundFilePath = null;
      foreach (var directory in _promptsDirectories)
      {
        var candidatePath = Path.Combine(directory, fileName);
        if (File.Exists(candidatePath))
        {
          foundFilePath = candidatePath;
          _logger.LogDebug("Found prompt file at: {Path}", candidatePath);
          break;
        }
        else
        {
          _logger.LogTrace("Prompt file not found at: {Path}", candidatePath);
        }
      }

      if (foundFilePath == null)
      {
        _logger.LogWarning(
            "Prompt file '{FileName}' not found in any search directory for key '{Key}'. Searched paths: {SearchPaths}",
            fileName,
            key.Value,
            string.Join(", ", _promptsDirectories));
        return GetFallbackPrompt(key);
      }

      // Read file content
      var content = await File.ReadAllTextAsync(foundFilePath, cancellationToken);

      if (string.IsNullOrWhiteSpace(content))
      {
        _logger.LogWarning(
            "Prompt file '{Path}' is empty for key '{Key}'. Using fallback.",
            foundFilePath,
            key.Value);
        return GetFallbackPrompt(key);
      }

      // Cache the content
      _promptCache[key.Value] = content;

      _logger.LogInformation(
          "Loaded system prompt '{Key}' from file: {Path} ({Length} characters)",
          key.Value,
          foundFilePath,
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

  /// <summary>
  /// Diagnostic method to check which prompt files are actually available
  /// </summary>
  public Dictionary<string, string?> GetAvailablePrompts()
  {
    var available = new Dictionary<string, string?>();

    foreach (var kvp in _promptFileMap)
    {
      var key = kvp.Key;
      var fileName = kvp.Value;

      string? foundPath = null;
      foreach (var directory in _promptsDirectories)
      {
        var candidatePath = Path.Combine(directory, fileName);
        if (File.Exists(candidatePath))
        {
          foundPath = candidatePath;
          break;
        }
      }

      available[key] = foundPath;
    }

    return available;
  }

  private Result<string> GetFallbackPrompt(SystemPromptKey key)
  {
    // Provide key-specific fallback prompts
    var fallbackContent = key.Value switch
    {
      "shopping-assistant" => @"You are **Aiva**, an intelligent shopping assistant AI.

**Your Role:**
- Help customers find products and make purchase decisions
- Provide detailed product information and comparisons
- Assist with cart management and checkout process
- Use available tools to search products and manage shopping cart

**Instructions:**
- Always search for products using the search_infors tool when customers ask about products
- Use get_product_info for detailed information about specific products
- Help customers add items to cart using add_to_cart tool
- Use get_cart to show cart contents when asked
- Guide customers through checkout when they're ready to purchase
- Be helpful, friendly, and focus on providing excellent customer service

**Available Tools:**
- search_infors: Search for products
- get_product_info: Get detailed product information  
- add_to_cart: Add products to shopping cart
- get_cart: View cart contents
- remove_from_cart: Remove items from cart
- checkout: Complete purchase",

      "data-assistant" => @"You are **Aiva**, an intelligent data assistant AI.

**Your Role:**
- Help analyze and query business data from SQL databases
- Generate charts and visualizations from data
- Provide insights and reports for business decision making
- Execute SQL queries safely and efficiently

**Instructions:**
- Write clear, optimized SQL queries
- Always validate data before generating reports
- Create meaningful visualizations when appropriate
- Explain your analysis in business terms
- Never execute destructive operations (DELETE, DROP, etc.)
- Use proper error handling and data validation",

      _ => @"You are **Aiva**, an intelligent AI assistant.

**Your Role:**
- Provide accurate, helpful information based on available data
- Always cite your sources when using retrieved documents
- Be professional, friendly, and empathetic
- Never make up information - only use verified data

**Instructions:**
- Answer questions clearly and concisely
- If information is not found, admit it and offer alternatives
- Use retrieved context to ground your responses
- Maintain a helpful and solution-oriented tone"
    };

    _logger.LogInformation(
        "Using fallback prompt for key '{Key}' ({Length} characters)",
        key.Value,
        fallbackContent.Length);

    return Result.Success(fallbackContent);
  }
}
