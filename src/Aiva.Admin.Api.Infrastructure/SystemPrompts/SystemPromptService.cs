using Ardalis.Result;
using Microsoft.Extensions.Caching.Memory;

namespace Aiva.Admin.Api.Infrastructure.SystemPrompts;

using Configuration;
using Core.Interfaces;
using Core.SystemPromptAggregate;
using Core.SystemPromptAggregate.Specifications;

public sealed class SystemPromptService : ISystemPromptService
{
  private readonly IRepository<SystemPrompt> _repository;
  private readonly IMemoryCache _cache;
  private readonly AppSettings _appSettings;
  private readonly ILogger<SystemPromptService> _logger;
  private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

  public SystemPromptService(
      IRepository<SystemPrompt> repository,
      IMemoryCache cache,
      AppSettings appSettings,
      ILogger<SystemPromptService> logger)
  {
    _repository = repository;
    _cache = cache;
    _appSettings = appSettings;
    _logger = logger;
  }

  public async Task<Result<string>> GetActivePromptContentAsync(
      SystemPromptKey key,
      CancellationToken cancellationToken = default)
  {
    var cacheKey = $"SystemPrompt_{key.Value}";

    // Try to get from cache first
    if (_cache.TryGetValue<string>(cacheKey, out var cachedContent))
    {
      _logger.LogDebug("Retrieved system prompt '{Key}' from cache", key.Value);
      return Result.Success(cachedContent!);
    }

    // Get from database
    var spec = new ActiveSystemPromptByKeySpec(key);
    var systemPrompt = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (systemPrompt is null)
    {
      _logger.LogWarning("Active system prompt with key '{Key}' not found. Using fallback.", key.Value);
      return GetFallbackPrompt(key);
    }

    // Cache the content
    var cacheOptions = new MemoryCacheEntryOptions()
        .SetAbsoluteExpiration(CacheDuration)
        .SetPriority(CacheItemPriority.High);

    _cache.Set(cacheKey, systemPrompt.Content, cacheOptions);

    _logger.LogInformation(
        "Retrieved system prompt '{Key}' (v{Version}) from database and cached",
        key.Value,
        systemPrompt.Version);

    return Result.Success(systemPrompt.Content);
  }

  public async Task<Result<SystemPrompt>> GetActivePromptAsync(
      SystemPromptKey key,
      CancellationToken cancellationToken = default)
  {
    var spec = new ActiveSystemPromptByKeySpec(key);
    var systemPrompt = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (systemPrompt is null)
    {
      _logger.LogWarning("Active system prompt with key '{Key}' not found", key.Value);
      return Result.NotFound($"Active system prompt with key '{key.Value}' not found");
    }

    return Result.Success(systemPrompt);
  }

  public void InvalidateCache(SystemPromptKey key)
  {
    var cacheKey = $"SystemPrompt_{key.Value}";
    _cache.Remove(cacheKey);
    _logger.LogInformation("Invalidated cache for system prompt '{Key}'", key.Value);
  }

  public void ClearCache()
  {
    // Note: IMemoryCache doesn't have a Clear method
    // You might need to track keys or use IDistributedCache for this
    _logger.LogInformation("Cache clear requested (not fully implemented for IMemoryCache)");
  }

  private Result<string> GetFallbackPrompt(SystemPromptKey key)
  {
    // Fallback to appsettings default
    var fallbackContent = _appSettings.AzureAI.DefaultSystemPrompt;

    if (string.IsNullOrWhiteSpace(fallbackContent))
    {
      fallbackContent = "You are a helpful AI assistant for the Aiva Admin system.";
      _logger.LogWarning("No fallback prompt configured. Using hardcoded default.");
    }

    return Result.Success(fallbackContent);
  }
}
