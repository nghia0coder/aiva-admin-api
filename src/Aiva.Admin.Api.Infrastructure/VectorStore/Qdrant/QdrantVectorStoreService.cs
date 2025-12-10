using Ardalis.Result;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Aiva.Admin.Api.Infrastructure.VectorStore.Qdrant;

using Core.Commons.Models;
using Core.Interfaces;
using Infrastructure.Data.Config;


/// <summary>
/// Qdrant implementation of vector store service
/// </summary>
public sealed class QdrantVectorStoreService : IVectorStoreService
{
  private readonly QdrantClient _client;
  private readonly QdrantConfiguration _configuration;
  private readonly ILogger<QdrantVectorStoreService> _logger;

  public QdrantVectorStoreService(
      QdrantClient client,
      IOptions<QdrantConfiguration> options,
      ILogger<QdrantVectorStoreService> logger)
  {
    _client = client;
    _configuration = options.Value;
    _logger = logger;
  }

  public async Task<Result> EnsureCollectionExistsAsync(
      string collectionName,
      int vectorDimension,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var exists = await _client.CollectionExistsAsync(collectionName, cancellationToken);

      if (!exists)
      {
        await _client.CreateCollectionAsync(
            collectionName,
            new VectorParams
            {
              Size = (ulong)vectorDimension,
              Distance = Distance.Cosine
            },
            cancellationToken: cancellationToken);

        // Create payload indexes for filtering
        await _client.CreatePayloadIndexAsync(
            collectionName,
            "documentId",
            PayloadSchemaType.Keyword,
            cancellationToken: cancellationToken);

        await _client.CreatePayloadIndexAsync(
            collectionName,
            "storageId",
            PayloadSchemaType.Keyword,
            cancellationToken: cancellationToken);

        await _client.CreatePayloadIndexAsync(
            collectionName,
            "folderId",
            PayloadSchemaType.Keyword,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Created Qdrant collection {CollectionName} with dimension {Dimension}",
            collectionName, vectorDimension);
      }

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to ensure Qdrant collection {CollectionName}", collectionName);
      return Result.Error($"Failed to create collection: {ex.Message}");
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

      var points = chunks.Select(chunk => new PointStruct
      {
        Id = new PointId { Uuid = chunk.Id },
        Vectors = chunk.Embedding.ToArray(),
        Payload =
        {
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
          ["lastModified"] = chunk.Metadata.LastModified?.ToString("O") ?? ""
        }
      }).ToList();

      await _client.UpsertAsync(
          collectionName,
          points,
          cancellationToken: cancellationToken);

      _logger.LogInformation(
          "Upserted {Count} chunks to Qdrant collection {CollectionName}",
          chunks.Count, collectionName);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to upsert chunks to Qdrant collection {CollectionName}", collectionName);
      return Result.Error($"Failed to upsert chunks: {ex.Message}");
    }
  }

  public async Task<Result<IReadOnlyList<VectorSearchResult>>> SearchAsync(
      string collectionName,
      ReadOnlyMemory<float> queryVector,
      VectorSearchOptions options,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var filter = BuildFilter(options);

      var searchResults = await _client.SearchAsync(
          collectionName,
          queryVector.ToArray(),
          limit: (ulong)options.TopK,
          filter: filter,
          scoreThreshold: options.MinScore.HasValue ? (float)options.MinScore.Value : null,
          cancellationToken: cancellationToken);

      var results = searchResults.Select(r => new VectorSearchResult
      {
        ChunkId = r.Id.Uuid,
        DocumentId = r.Payload["documentId"].StringValue,
        Content = r.Payload["content"].StringValue,
        Score = r.Score,
        Metadata = new DocumentChunkMetadata
        {
          FileName = GetStringOrNull(r.Payload, "fileName"),
          PageNumber = GetIntOrNull(r.Payload, "pageNumber"),
          SectionTitle = GetStringOrNull(r.Payload, "sectionTitle"),
          StorageId = GetIntOrNull(r.Payload, "storageId"),
          FolderId = GetIntOrNull(r.Payload, "folderId"),
          TotalChunks = GetIntOrNull(r.Payload, "totalChunks"),
          CharacterOffset = GetIntOrNull(r.Payload, "characterOffset"),
          ContentType = GetStringOrNull(r.Payload, "contentType")
        }
      }).ToList();

      _logger.LogDebug(
          "Found {Count} results in Qdrant collection {CollectionName}",
          results.Count, collectionName);

      return Result.Success<IReadOnlyList<VectorSearchResult>>(results);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to search Qdrant collection {CollectionName}", collectionName);
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
      var filter = new Filter
      {
        Must =
        {
          new Condition
          {
            Field = new FieldCondition
            {
              Key = "documentId",
              Match = new Match { Keyword = documentId }
            }
          }
        }
      };

      await _client.DeleteAsync(
          collectionName,
          filter,
          cancellationToken: cancellationToken);

      _logger.LogInformation(
          "Deleted chunks for document {DocumentId} from Qdrant collection {CollectionName}",
          documentId, collectionName);

      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex,
          "Failed to delete document {DocumentId} from Qdrant collection {CollectionName}",
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
      await _client.DeleteCollectionAsync(collectionName, null, cancellationToken);

      _logger.LogInformation("Deleted Qdrant collection {CollectionName}", collectionName);
      return Result.Success();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to delete Qdrant collection {CollectionName}", collectionName);
      return Result.Error($"Delete collection failed: {ex.Message}");
    }
  }

  public async Task<bool> CollectionExistsAsync(
      string collectionName,
      CancellationToken cancellationToken = default)
  {
    return await _client.CollectionExistsAsync(collectionName, cancellationToken);
  }

  private static Filter? BuildFilter(VectorSearchOptions options)
  {
    var conditions = new List<Condition>();

    if (options.DocumentIds?.Count > 0)
    {
      var keywords = new RepeatedStrings();
      keywords.Strings.AddRange(options.DocumentIds);
      conditions.Add(new Condition
      {
        Field = new FieldCondition
        {
          Key = "documentId",
          Match = new Match
          {
            Keywords = keywords
          }
        }
      });
    }

    if (options.StorageId.HasValue)
    {
      conditions.Add(new Condition
      {
        Field = new FieldCondition
        {
          Key = "storageId",
          Match = new Match { Keyword = options.StorageId.Value.ToString() }
        }
      });
    }

    if (options.FolderId.HasValue)
    {
      conditions.Add(new Condition
      {
        Field = new FieldCondition
        {
          Key = "folderId",
          Match = new Match { Keyword = options.FolderId.Value.ToString() }
        }
      });
    }

    if (conditions.Count == 0)
    {
      return null;
    }

    var filter = new Filter();
    filter.Must.AddRange(conditions);
    return filter;
  }

  private static string? GetStringOrNull(
      Google.Protobuf.Collections.MapField<string, Value> payload,
      string key)
  {
    return payload.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.StringValue)
        ? value.StringValue
        : null;
  }

  private static int? GetIntOrNull(
      Google.Protobuf.Collections.MapField<string, Value> payload,
      string key)
  {
    return payload.TryGetValue(key, out var value) && value.IntegerValue != 0
        ? (int)value.IntegerValue
        : null;
  }

  private static Guid? GetGuidOrNull(
      Google.Protobuf.Collections.MapField<string, Value> payload,
      string key)
  {
    if (payload.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.StringValue))
    {
      return Guid.TryParse(value.StringValue, out var guid) ? guid : null;
    }
    return null;
  }
}
