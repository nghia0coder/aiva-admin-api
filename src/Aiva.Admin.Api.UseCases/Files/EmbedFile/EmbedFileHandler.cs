using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Aiva.Admin.Api.UseCases.Files.EmbedFile;

using Ardalis.Result;
using Core.Commons.Models;
using Core.Commons.Results;
using Core.FileAggregate;
using Core.FileAggregate.Specifications;
using Core.Interfaces;


public sealed class EmbedFileHandler(
    IRepository<FileMetadata> metadataRepository,
    IReadRepository<File> fileRepository,
    IEmbeddingService embeddingService,
    IVectorStoreService vectorStoreService,
    IChunkingService chunkingService,
    IVectorStoreSettings vectorStoreConfig,
    ILogger<EmbedFileHandler> logger)
    : ICommandHandler<EmbedFileCommand, Result<EmbedFileResult>>
{
  private readonly IVectorStoreSettings _config = vectorStoreConfig;

  public async ValueTask<Result<EmbedFileResult>> Handle(
      EmbedFileCommand command,
      CancellationToken cancellationToken)
  {
    // 1. Get file and metadata
    var file = await fileRepository.GetByIdAsync(command.FileId, cancellationToken);
    if (file is null)
      return Result.NotFound($"File {command.FileId.Value} not found");

    var metadataSpec = new FileMetadataByFileIdSpec(file.Id);
    var metadata = await metadataRepository.FirstOrDefaultAsync(metadataSpec, cancellationToken);
    if (metadata is null)
      return Result.NotFound($"FileMetadata for {command.FileId.Value} not found");

    // 2. Validate: must be extracted first
    if (metadata.Status != FileProcessingStatus.Completed)
    {
      return new EmbedFileResult(
          file.Id.Value, 0, _config.DefaultCollectionName, false,
          $"File must be processed first. Current status: {metadata.Status.Name}");
    }

    if (string.IsNullOrWhiteSpace(metadata.ExtractedText))
    {
      return new EmbedFileResult(
          file.Id.Value, 0, _config.DefaultCollectionName, false,
          "No extracted text available");
    }

    // 3. Skip if already embedded (idempotent)
    if (metadata.IsEmbedded)
    {
      logger.LogDebug("File {FileId} already embedded, skipping", file.Id.Value);
      return new EmbedFileResult(file.Id.Value, 0, _config.DefaultCollectionName, true);
    }

    try
    {
      // 4. Ensure collection exists
      var collectionName = _config.DefaultCollectionName;
      var ensureResult = await vectorStoreService.EnsureCollectionExistsAsync(
          collectionName,
          embeddingService.EmbeddingDimension,
          cancellationToken);

      if (!ensureResult.IsSuccess)
        return Result.Error(string.Join(", ", ensureResult.Errors));

      // 5. Chunk text
      var chunks = chunkingService.ChunkText(metadata.ExtractedText);
      logger.LogInformation("File {FileId}: {ChunkCount} chunks", file.Id.Value, chunks.Count);

      // 6. Generate embeddings (batch)
      var contents = chunks.Select(c => c.Content).ToList();
      var embeddingsResult = await embeddingService.GenerateEmbeddingsAsync(contents, cancellationToken);

      if (!embeddingsResult.IsSuccess)
        return Result.Error(string.Join(", ", embeddingsResult.Errors));

      // 7. Extract product info (Hybrid: from fileName + content)
      var productInfo = ExtractProductInfo(file.OriginalFileName.Value, metadata.ExtractedText);

      // 8. Build document chunks
      var documentChunks = chunks.Zip(embeddingsResult.Value, (chunk, embedding) =>
          new DocumentChunk
          {
            Id = $"{file.Id.Value}_{chunk.Index}",
            DocumentId = file.Id.Value.ToString(),
            Content = chunk.Content,
            Embedding = embedding,
            ChunkIndex = chunk.Index,
            Metadata = new DocumentChunkMetadata
            {
              FileName = file.OriginalFileName.Value,
              StorageId = file.StorageId.Value,
              FolderId = file.FolderId.Value,
              TotalChunks = chunks.Count,
              CharacterOffset = chunk.CharacterOffset,
              ContentType = file.ContentType,
              LastModified = file.ModifiedOnUtc ?? file.CreatedOnUtc,
              ProductId = productInfo.ProductId,
              ProductName = productInfo.ProductName,
              Category = productInfo.Category,
              Brand = productInfo.Brand,
              Price = productInfo.Price,
              Tags = productInfo.Tags
            }
          }).ToList();

      // 9. Upsert to vector store
      var upsertResult = await vectorStoreService.UpsertChunksAsync(
          collectionName, documentChunks, cancellationToken);

      if (!upsertResult.IsSuccess)
        return Result.Error(string.Join(", ", upsertResult.Errors));

      // 10. Mark as embedded
      metadata.MarkAsEmbedded();
      await metadataRepository.UpdateAsync(metadata, cancellationToken);

      logger.LogInformation("File {FileId} embedded: {ChunkCount} chunks", file.Id.Value, chunks.Count);

      return new EmbedFileResult(file.Id.Value, chunks.Count, collectionName, true);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Embedding failed for file {FileId}", file.Id.Value);
      return new EmbedFileResult(file.Id.Value, 0, _config.DefaultCollectionName, false, ex.Message);
    }
  }

  /// <summary>
  /// Extracts product information using hybrid approach (fileName + content)
  /// </summary>
  private static ProductInfo ExtractProductInfo(string fileName, string content)
  {
    var infoFromFileName = ExtractProductInfoFromFileName(fileName);
    var infoFromContent = ExtractProductInfoFromContent(content);

    // Merge: fileName takes priority for ID/Name, content for other fields
    return new ProductInfo
    {
      ProductId = infoFromFileName.ProductId ?? infoFromContent.ProductId,
      ProductName = infoFromFileName.ProductName ?? infoFromContent.ProductName,
      Brand = infoFromContent.Brand ?? infoFromFileName.Brand,
      Category = infoFromContent.Category ?? infoFromFileName.Category,
      Price = infoFromContent.Price ?? infoFromFileName.Price,
      Tags = infoFromContent.Tags ?? infoFromFileName.Tags
    };
  }

  /// <summary>
  /// Extracts product information from file name
  /// Supports patterns: "product-{id}-{name}.md", "PROD-{id}-{name}.md", "{name}-{id}.md"
  /// </summary>
  private static ProductInfo ExtractProductInfoFromFileName(string fileName)
  {
    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
    var parts = nameWithoutExt.Split('-', StringSplitOptions.RemoveEmptyEntries);

    string? productId = null;
    string? productName = null;

    if (parts.Length >= 3 && parts[0].Equals("product", StringComparison.OrdinalIgnoreCase))
    {
      // Pattern: product-{id}-{name...}
      productId = parts[1];
      productName = string.Join(" ", parts.Skip(2));
    }
    else if (parts.Length >= 2 && parts[0].StartsWith("PROD", StringComparison.OrdinalIgnoreCase))
    {
      // Pattern: PROD-{id}-{name...} or PROD-001-{name...}
      productId = parts[0]; // Full "PROD-001" or "PROD001"
      productName = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : parts[1];
    }
    else if (parts.Length >= 2)
    {
      // Pattern: {name}-{id} (last part is ID)
      if (Regex.IsMatch(parts[^1], @"^\d+$")) // Last part is numeric ID
      {
        productId = parts[^1];
        productName = string.Join(" ", parts.Take(parts.Length - 1));
      }
      else
      {
        // Fallback: first part as ID, rest as name
        productId = parts[0];
        productName = string.Join(" ", parts.Skip(1));
      }
    }
    else if (parts.Length == 1)
    {
      productName = parts[0];
    }

    // Extract brand from productName (heuristic)
    var brand = ExtractBrandFromName(productName);

    return new ProductInfo
    {
      ProductId = productId,
      ProductName = productName,
      Brand = brand,
      Category = null,
      Price = null,
      Tags = null
    };
  }

  /// <summary>
  /// Extracts product information from content (MD headers and structured text)
  /// Supports patterns: **Product ID:**, **Brand:**, **Category:**, **Price:**, etc.
  /// </summary>
  private static ProductInfo ExtractProductInfoFromContent(string content)
  {
    var productInfo = new ProductInfo();

    // Extract Product ID
    var productIdMatch = Regex.Match(
        content,
        @"\*\*Product\s+ID:\*\*\s*([A-Z0-9-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Multiline);
    if (productIdMatch.Success)
      productInfo = productInfo with { ProductId = productIdMatch.Groups[1].Value.Trim() };

    // Extract Product Name from H1
    var nameMatch = Regex.Match(
        content,
        @"^#\s+(.+)$",
        RegexOptions.Multiline);
    if (nameMatch.Success)
      productInfo = productInfo with { ProductName = nameMatch.Groups[1].Value.Trim() };

    // Extract Brand
    var brandMatch = Regex.Match(
        content,
        @"\*\*Brand:\*\*\s*([^\n*]+)",
        RegexOptions.IgnoreCase);
    if (brandMatch.Success)
      productInfo = productInfo with { Brand = brandMatch.Groups[1].Value.Trim() };

    // Extract Category
    var categoryMatch = Regex.Match(
        content,
        @"\*\*Category:\*\*\s*([^\n*]+)",
        RegexOptions.IgnoreCase);
    if (categoryMatch.Success)
      productInfo = productInfo with { Category = categoryMatch.Groups[1].Value.Trim() };

    // Extract Price
    var priceMatch = Regex.Match(
        content,
        @"\*\*Price:\*\*\s*\$?(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)",
        RegexOptions.IgnoreCase);
    if (priceMatch.Success)
    {
      var priceStr = priceMatch.Groups[1].Value.Replace(",", "");
      if (double.TryParse(priceStr, out var price))
        productInfo = productInfo with { Price = price };
    }

    // Extract Tags (if present)
    var tagsMatch = Regex.Match(
        content,
        @"\*\*Tags?:\*\*\s*([^\n*]+)",
        RegexOptions.IgnoreCase);
    if (tagsMatch.Success)
    {
      var tagsStr = tagsMatch.Groups[1].Value.Trim();
      var tags = tagsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
          .Select(t => t.Trim())
          .Where(t => !string.IsNullOrWhiteSpace(t))
          .ToList();
      if (tags.Count > 0)
        productInfo = productInfo with { Tags = tags };
    }

    return productInfo;
  }

  /// <summary>
  /// Extracts brand name from product name using known brands
  /// </summary>
  private static string? ExtractBrandFromName(string? productName)
  {
    if (string.IsNullOrWhiteSpace(productName))
      return null;

    var knownBrands = new[]
    {
      "Apple", "Samsung", "Google", "Xiaomi", "OnePlus", "Sony", "LG",
      "Huawei", "Oppo", "Vivo", "Motorola", "Nokia", "Realme", "Asus"
    };

    foreach (var brand in knownBrands)
    {
      if (productName.Contains(brand, StringComparison.OrdinalIgnoreCase))
        return brand;
    }

    return null;
  }

  /// <summary>
  /// Product information extracted from file
  /// </summary>
  private sealed record ProductInfo
  {
    public string? ProductId { get; init; }
    public string? ProductName { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public double? Price { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
  }
}
