namespace Aiva.Admin.Api.Core.Interfaces;

using SystemPromptAggregate;

public interface ISystemPromptService
{
  /// <summary>
  /// Gets the active system prompt by key, with caching
  /// </summary>
  Task<Result<string>> GetActivePromptContentAsync(
      SystemPromptKey key,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets the active system prompt entity by key
  /// </summary>
  Task<Result<SystemPrompt>> GetActivePromptAsync(
      SystemPromptKey key,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Invalidates cache for a specific prompt key
  /// </summary>
  void InvalidateCache(SystemPromptKey key);

  /// <summary>
  /// Clears all cached prompts
  /// </summary>
  void ClearCache();
}
