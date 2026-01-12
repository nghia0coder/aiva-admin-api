using Ardalis.Result;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace Aiva.Admin.Api.Infrastructure.VectorStore.AzureAISearch;

using Configuration;
using Core.Commons.Models;
using Core.Interfaces;
using Data.Config;

/// <summary>
/// Azure AI Search implementation of vector store service
/// </summary>
public sealed class AzureAISearchVectorStoreService : IVectorStoreService
{
  private readonly SearchIndexClient _indexClient;
  private readonly AzureAISearchConfiguration _configuration;
  private readonly ILogger<AzureAISearchVectorStoreService> _logger;
  private readonly Dictionary<string, SearchClient> _searchClients = new();
  private readonly AppSettings _appSettings;

  public AzureAISearchVectorStoreService(
      IOptions<AzureAISearchConfiguration> options,
      ILogger<AzureAISearchVectorStoreService> logger,
      AppSettings appSettings)
  {
    _configuration = options.Value;
    _logger = logger;
    _appSettings = appSettings;
    _indexClient = CreateIndexClient();
  }

  private SearchIndexClient CreateIndexClient()
  {
    var endpoint = new Uri(_appSettings.AzureAISearch.Endpoint);

    if (_configuration.UseManagedIdentity)
    {
      return new SearchIndexClient(endpoint, new DefaultAzureCredential());
    }

    if (!string.IsNullOrEmpty(_appSettings.AzureAISearch.ApiKey))
    {
      return new SearchIndexClient(endpoint, new AzureKeyCredential(_appSettings.AzureAISearch.ApiKey));
    }

    throw new InvalidOperationException(
        "Azure AI Search configuration is invalid. " +
        "Provide either ApiKey or enable UseManagedIdentity.");
  }

  private SearchClient GetSearchClient(string indexName)
  {
    if (!_searchClients.TryGetValue(indexName, out var client))
    {
      var endpoint = new Uri(_appSettings.AzureAISearch.Endpoint);

      client = _appSettings.AzureAISearch.UseManagedIdentity
          ? new SearchClient(endpoint, indexName, new DefaultAzureCredential())
          : new SearchClient(endpoint, indexName, new AzureKeyCredential(_appSettings.AzureAISearch.ApiKey!));

      _searchClients[indexName] = client;
    }

    return client;
  }

  public async Task<Result> EnsureCollectionExistsAsync(
      string collectionName,
      int vectorDimension,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var indexName = NormalizeIndexName(collectionName);

      try
      {
        await _indexClient.GetIndexAsync(indexName, cancellationToken);
        return Result.Success(); // Index already exists
      }
      catch (RequestFailedException ex) when (ex.Status == 404)
      {
        // Index doesn't exist, create it
      }

      var index = new SearchIndex(indexName)
      {
        Fields =
        [
          new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
          new SearchableField("documentId") { IsFilterable = true },
          new SearchableField("content"),
          new SimpleField("chunkIndex", SearchFieldDataType.Int32) { IsFilterable = true },
          new SearchableField("fileName") { IsFilterable = true },
          new SimpleField("pageNumber", SearchFieldDataType.Int32) { IsFilterable = true },
          new SearchableField("sectionTitle"),
          new SimpleField("storageId", SearchFieldDataType.String) { IsFilterable = true },
          new SimpleField("folderId", SearchFieldDataType.String) { IsFilterable = true },
          new SimpleField("totalChunks", SearchFieldDataType.Int32),
          new SimpleField("characterOffset", SearchFieldDataType.Int32),
          new SearchableField("contentType") { IsFilterable = true },
          new SimpleField("lastModified", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
          new VectorSearchField("embedding", vectorDimension, "vector-profile"),
          new SearchableField("productId")
          {
              IsFilterable = true,
              AnalyzerName = LexicalAnalyzerName.Keyword
          },
          new SearchableField("productName")
          {
              AnalyzerName = LexicalAnalyzerName.EnMicrosoft  // semantic search
          },
          new SearchableField("category")
          {
              IsFilterable = true,
              IsFacetable = true,
              AnalyzerName = LexicalAnalyzerName.Keyword  // exact match
          },
          new SearchableField("brand")
          {
              IsFilterable = true,
              IsFacetable = true,
              AnalyzerName = LexicalAnalyzerName.Keyword  // exact match
          },
          new SimpleField("price", SearchFieldDataType.Double)
          {
              IsFilterable = true,
              IsSortable = true,
              IsFacetable = true,
          },
          new SearchableField("tags")
          {
              IsFilterable = true,
              IsFacetable = true
              // Removed IsRetrievable = true, as SearchableField does not have this property
          }
        ],
        VectorSearch = new VectorSearch
        {
          Profiles =
          {
            new VectorSearchProfile("vector-profile", "hnsw-config")
          },
          Algorithms =
          {
            new HnswAlgorithmConfiguration("hnsw-config")
            {
              Parameters = new HnswParameters
              {
                Metric = VectorSearchAlgorithmMetric.Cosine,
                M = 4,
                EfConstruction = 400,
                EfSearch = 500
              }
            }
          }
        },
        SemanticSearch = new SemanticSearch
        {
          Configurations =
          {
              new SemanticConfiguration(_configuration.SemanticConfigurationName,
                  new SemanticPrioritizedFields
                  {
                      ContentFields = { new SemanticField("content"), new SemanticField("productName") },
                      TitleField = new SemanticField("productName"),
                      KeywordsFields = { new SemanticField("category"), new SemanticField("brand"), new SemanticField("tags") }
                  })
          }
        }
      };

      await _indexClient.CreateIndexAsync(index, cancellationToken);

      _logger.LogInformation(
          "Created Azure AI Search index {IndexName} with dimension {Dimension}",
          indexName, vectorDimension);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to ensure Azure AI Search index {IndexName}", collectionName);
      return Result.Error($"Failed to create index: {ex.Message}");
    }
  }

  public async Task<Result> UpsertChunksAsync(
      string collectionName,
      IReadOnlyList<DocumentChunk> chunks,
      CancellationToken cancellationToken = default)
  {
    try
    {
      if (chunks.Count == 0)
      {
        return Result.Success();
      }

      var indexName = NormalizeIndexName(collectionName);
      var searchClient = GetSearchClient(indexName);

      var documents = chunks.Select(chunk => new SearchDocument
      {
        ["id"] = chunk.Id,
        ["documentId"] = chunk.DocumentId,
        ["content"] = chunk.Content,
        ["chunkIndex"] = chunk.ChunkIndex,
        ["fileName"] = chunk.Metadata.FileName ?? "",
        ["pageNumber"] = chunk.Metadata.PageNumber ?? 0,
        ["sectionTitle"] = chunk.Metadata.SectionTitle ?? "",
        ["storageId"] = chunk.Metadata.StorageId?.ToString() ?? "",
        ["folderId"] = chunk.Metadata.FolderId?.ToString() ?? "",
        ["totalChunks"] = chunk.Metadata.TotalChunks ?? 0,
        ["characterOffset"] = chunk.Metadata.CharacterOffset ?? 0,
        ["contentType"] = chunk.Metadata.ContentType ?? "",
        ["lastModified"] = chunk.Metadata.LastModified,
        ["embedding"] = chunk.Embedding.ToArray(),
        ["productId"] = chunk.Metadata.ProductId ?? "",
        ["productName"] = chunk.Metadata.ProductName ?? "",
        ["category"] = chunk.Metadata.Category ?? "",
        ["brand"] = chunk.Metadata.Brand ?? "",
        ["price"] = chunk.Metadata.Price ?? (double?)null,
        ["tags"] = chunk.Metadata.Tags?.ToArray() ?? Array.Empty<string>()
      }).ToList();

      // Batch upload (Azure AI Search supports up to 1000 documents per batch)
      const int batchSize = 1000;
      for (var i = 0; i < documents.Count; i += batchSize)
      {
        var batch = documents.Skip(i).Take(batchSize);
        await searchClient.MergeOrUploadDocumentsAsync(batch, cancellationToken: cancellationToken);
      }

      _logger.LogInformation(
          "Upserted {Count} chunks to Azure AI Search index {IndexName}",
          chunks.Count, indexName);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to upsert chunks to Azure AI Search index {IndexName}", collectionName);
      return Result.Error($"Failed to upsert chunks: {ex.Message}");
    }
  }

  public async Task<Result<IReadOnlyList<VectorSearchResult>>> SearchAsync(
      string collectionName,
      ReadOnlyMemory<float> queryVector,
      Core.Commons.Models.VectorSearchOptions options,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var indexName = NormalizeIndexName(collectionName);
      var searchClient = GetSearchClient(indexName);

      var vectorQuery = new VectorizedQuery(queryVector.ToArray())
      {
        KNearestNeighborsCount = options.TopK,
        Fields = { "embedding" }
      };

      var searchOptions = new SearchOptions
      {
        VectorSearch = new Azure.Search.Documents.Models.VectorSearchOptions
        {
          Queries = { vectorQuery }
        },
        Size = options.TopK,
        Select = { "id", "documentId", "content", "fileName", "pageNumber",
                   "sectionTitle", "storageId", "folderId", "totalChunks",
                   "characterOffset", "contentType" }
      };

      // Build filter
      var filterParts = new List<string>();

      if (options.DocumentIds?.Count > 0)
      {
        var docFilter = string.Join(" or ",
            options.DocumentIds.Select(id => $"documentId eq '{id}'"));
        filterParts.Add($"({docFilter})");
      }

      if (options.StorageId.HasValue)
      {
        filterParts.Add($"storageId eq '{options.StorageId.Value}'");
      }

      if (options.FolderId.HasValue)
      {
        filterParts.Add($"folderId eq '{options.FolderId.Value}'");
      }

      if (filterParts.Count > 0)
      {
        searchOptions.Filter = string.Join(" and ", filterParts);
      }

      var response = await searchClient.SearchAsync<SearchDocument>(
          null, // No text query, using vector only
          searchOptions,
          cancellationToken);

      var results = new List<VectorSearchResult>();
      await foreach (var result in response.Value.GetResultsAsync())
      {
        // Filter by minimum score if specified
        if (options.MinScore.HasValue && result.Score < options.MinScore.Value)
        {
          continue;
        }

        results.Add(new VectorSearchResult
        {
          ChunkId = result.Document["id"]?.ToString() ?? "",
          DocumentId = result.Document["documentId"]?.ToString() ?? "",
          Content = result.Document["content"]?.ToString() ?? "",
          Score = result.Score ?? 0,
          Metadata = new DocumentChunkMetadata
          {
            FileName = result.Document["fileName"]?.ToString(),
            PageNumber = result.Document["pageNumber"] is int pn ? pn : null,
            SectionTitle = result.Document["sectionTitle"]?.ToString(),
            StorageId = Int32.TryParse(result.Document["storageId"]?.ToString(), out var sid) ? sid : null,
            FolderId = Int32.TryParse(result.Document["folderId"]?.ToString(), out var fid) ? fid : null,
            TotalChunks = result.Document["totalChunks"] is int tc ? tc : null,
            CharacterOffset = result.Document["characterOffset"] is int co ? co : null,
            ContentType = result.Document["contentType"]?.ToString()
          }
        });
      }

      _logger.LogDebug(
          "Found {Count} results in Azure AI Search index {IndexName}",
          results.Count, indexName);

      return Result.Success<IReadOnlyList<VectorSearchResult>>(results);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to search Azure AI Search index {IndexName}", collectionName);
      return Result.Error($"Search failed: {ex.Message}");
    }
  }

  public async Task<Result> DeleteByDocumentIdAsync(
      string collectionName,
      string documentId,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var indexName = NormalizeIndexName(collectionName);
      var searchClient = GetSearchClient(indexName);

      // First, find all chunks for this document
      var searchOptions = new SearchOptions
      {
        Filter = $"documentId eq '{documentId}'",
        Select = { "id" },
        Size = 1000
      };

      var response = await searchClient.SearchAsync<SearchDocument>(
          "*", searchOptions, cancellationToken);

      var idsToDelete = new List<string>();
      await foreach (var result in response.Value.GetResultsAsync())
      {
        if (result.Document["id"]?.ToString() is { } id)
        {
          idsToDelete.Add(id);
        }
      }

      if (idsToDelete.Count > 0)
      {
        var batch = IndexDocumentsBatch.Delete("id", idsToDelete);
        await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
      }

      _logger.LogInformation(
          "Deleted {Count} chunks for document {DocumentId} from Azure AI Search index {IndexName}",
          idsToDelete.Count, documentId, indexName);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex,
          "Failed to delete document {DocumentId} from Azure AI Search index {IndexName}",
          documentId, collectionName);
      return Result.Error($"Delete failed: {ex.Message}");
    }
  }

  public async Task<Result> DeleteCollectionAsync(
      string collectionName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var indexName = NormalizeIndexName(collectionName);
      await _indexClient.DeleteIndexAsync(indexName, cancellationToken);

      _searchClients.Remove(indexName);

      _logger.LogInformation("Deleted Azure AI Search index {IndexName}", indexName);
      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to delete Azure AI Search index {IndexName}", collectionName);
      return Result.Error($"Delete index failed: {ex.Message}");
    }
  }

  public async Task<bool> CollectionExistsAsync(
      string collectionName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var indexName = NormalizeIndexName(collectionName);
      await _indexClient.GetIndexAsync(indexName, cancellationToken);
      return true;
    }
    catch (RequestFailedException ex) when (ex.Status == 404)
    {
      return false;
    }
  }

  /// <summary>
  /// Azure AI Search index names must be lowercase and can only contain letters, numbers, and dashes
  /// </summary>
  private static string NormalizeIndexName(string name)
  {
    return name.ToLowerInvariant()
        .Replace("_", "-")
        .Replace(".", "-");
  }
}
