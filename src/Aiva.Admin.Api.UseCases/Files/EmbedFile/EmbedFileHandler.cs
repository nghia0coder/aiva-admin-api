using Microsoft.Extensions.Logging;

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

      // 7. Build document chunks
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
              LastModified = file.ModifiedOnUtc ?? file.CreatedOnUtc
            }
          }).ToList();

      // 8. Upsert to vector store
      var upsertResult = await vectorStoreService.UpsertChunksAsync(
          collectionName, documentChunks, cancellationToken);

      if (!upsertResult.IsSuccess)
        return Result.Error(string.Join(", ", upsertResult.Errors));

      // 9. Mark as embedded
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
}
