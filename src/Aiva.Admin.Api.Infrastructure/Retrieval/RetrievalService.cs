using System.Text;
using Ardalis.Result;

namespace Aiva.Admin.Api.Infrastructure.Retrieval;

using Core.Commons.Models;
using Core.Commons.Results;
using Core.Interfaces;

public sealed class RetrievalService : IRetrievalService
{
  private readonly IVectorStoreService _vectorStoreService;
  private readonly IEmbeddingService _embeddingService;
  private readonly IVectorStoreSettings _settings;
  private readonly ILogger<RetrievalService> _logger;

  public RetrievalService(
      IVectorStoreService vectorStoreService,
      IEmbeddingService embeddingService,
      IVectorStoreSettings settings,
      ILogger<RetrievalService> logger)
  {
    _vectorStoreService = vectorStoreService;
    _embeddingService = embeddingService;
    _settings = settings;
    _logger = logger;
  }

  public async Task<Result<RetrievalResult>> RetrieveContextAsync(
      string query,
      RetrievalOptions? options = null,
      CancellationToken cancellationToken = default)
  {
    options ??= new RetrievalOptions();

    _logger.LogInformation(
        "Starting retrieval. Query: {Query}, Strategy: {Strategy}, Collection: {Collection}, TopK: {TopK}, MinScore: {MinScore}",
        query,
        options.Strategy,
        _settings.DefaultCollectionName,
        options.TopK,
        options.MinScore);

    // 1. Generate embedding for the query
    _logger.LogDebug("Generating embedding for query: {Query}", query);
    var embeddingResult = await _embeddingService.GenerateEmbeddingAsync(
        query, cancellationToken);

    if (!embeddingResult.IsSuccess)
    {
      _logger.LogError(
          "Failed to generate embedding. Query: {Query}, Errors: {Errors}",
          query,
          string.Join(", ", embeddingResult.Errors));
      return Result.Error(string.Join(", ", embeddingResult.Errors));
    }

    _logger.LogDebug(
        "Embedding generated successfully. Vector dimension: {Dimension}",
        embeddingResult.Value.Length);

    // 2. Execute search based on strategy
    _logger.LogInformation("Executing {Strategy} search on collection: {Collection}", 
        options.Strategy, _settings.DefaultCollectionName);
    
    var searchResult = options.Strategy switch
    {
      SearchStrategy.VectorOnly => await _vectorStoreService.SearchAsync(
          _settings.DefaultCollectionName,
          embeddingResult.Value,
          new VectorSearchOptions
          {
            TopK = options.TopK,
            MinScore = options.MinScore,
            StorageId = options.StorageId.HasValue ? Guid.Parse(options.StorageId.Value.ToString()) : null,
            FolderId = options.FolderId.HasValue ? Guid.Parse(options.FolderId.Value.ToString()) : null
          },
          cancellationToken),
      SearchStrategy.KeywordOnly => await SearchKeywordOnlyAsync(
          query, options, cancellationToken),
      SearchStrategy.Hybrid => await _vectorStoreService.HybridSearchAsync(
          _settings.DefaultCollectionName,
          query,                              // Text for keyword search
          embeddingResult.Value,              // Vector for semantic search
          new HybridSearchOptions
          {
            TopK = options.TopK,
            MinScore = options.MinScore,
            UseSemanticRanking = true,
            StorageId = options.StorageId.HasValue
                  ? Guid.Parse(options.StorageId.Value.ToString()) : null,
            FolderId = options.FolderId.HasValue
                  ? Guid.Parse(options.FolderId.Value.ToString()) : null
          },
          cancellationToken),
      _ => throw new ArgumentOutOfRangeException()
    };

    if (!searchResult.IsSuccess)
    {
      _logger.LogError(
          "Search failed. Query: {Query}, Strategy: {Strategy}, Errors: {Errors}",
          query,
          options.Strategy,
          string.Join(", ", searchResult.Errors));
      return Result.Error(string.Join(", ", searchResult.Errors));
    }

    _logger.LogInformation(
        "Search completed successfully. Found {Count} results",
        searchResult.Value.Count);

    // 3. Build formatted context for prompt injection
    var formattedContext = BuildContextString(searchResult.Value);
    var tokenCount = EstimateTokenCount(formattedContext);

    _logger.LogDebug(
        "Context built. TokenCount: {TokenCount}, ContextLength: {Length}",
        tokenCount,
        formattedContext.Length);

    return Result.Success(new RetrievalResult
    {
      Results = searchResult.Value,
      FormattedContext = formattedContext,
      TokenCount = tokenCount
    });
  }

  /// <summary>
  /// Performs keyword-only search using full-text search capabilities
  /// </summary>
  private async Task<Result<IReadOnlyList<VectorSearchResult>>> SearchKeywordOnlyAsync(
      string query,
      RetrievalOptions options,
      CancellationToken cancellationToken)
  {
    try
    {
      // For keyword-only search, we use hybrid search but with empty vector
      // This forces Azure AI Search to rely only on the text query component
      var emptyVector = new ReadOnlyMemory<float>(new float[1536]); // Assuming OpenAI embeddings

      return await _vectorStoreService.HybridSearchAsync(
          _settings.DefaultCollectionName,
          query,
          emptyVector,
          new HybridSearchOptions
          {
            TopK = options.TopK,
            MinScore = options.MinScore,
            UseSemanticRanking = false, // Disable semantic ranking for pure keyword search
            StorageId = options.StorageId.HasValue
                ? Guid.Parse(options.StorageId.Value.ToString()) : null,
            FolderId = options.FolderId.HasValue
                ? Guid.Parse(options.FolderId.Value.ToString()) : null
          },
          cancellationToken);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to perform keyword-only search for query: {Query}", query);
      return Result.Error($"Keyword search failed: {ex.Message}");
    }
  }

  private string BuildContextString(IReadOnlyList<VectorSearchResult> results)
  {
    var sb = new StringBuilder();
    sb.AppendLine("### Relevant Context:");
    sb.AppendLine();

    foreach (var (result, index) in results.Select((r, i) => (r, i)))
    {
      sb.AppendLine($"[Source {index + 1}: {result.Metadata?.FileName ?? "Unknown"}]");
      sb.AppendLine(result.Content);
      sb.AppendLine();
    }

    return sb.ToString();
  }

  /// <summary>
  /// Estimates token count for the formatted context
  /// Using rough approximation: 1 token ≈ 4 characters
  /// </summary>
  private static int EstimateTokenCount(string text)
  {
    if (string.IsNullOrEmpty(text))
      return 0;

    // Rough approximation: 1 token ≈ 4 characters for English text
    // This is conservative estimate for OpenAI models
    return (int)Math.Ceiling(text.Length / 4.0);
  }
}
