namespace Aiva.Admin.Api.UseCases.Files.ProcessFile;

using Core.FileAggregate;
using Core.FileAggregate.Specifications;
using Core.Interfaces;
using Core.StorageAggregate;
using Microsoft.Extensions.Logging;

public sealed class ProcessFileHandler(
    IRepository<FileMetadata> metadataRepository,
    IReadRepository<File> fileRepository,
    IReadRepository<Storage> storageRepository,
    IBlobStorageService blobStorageService,
    ITextExtractionService textExtractionService,
    ILogger<ProcessFileHandler> logger)
    : ICommandHandler<ProcessFileCommand, Result<ProcessFileResult>>
{
  public async ValueTask<Result<ProcessFileResult>> Handle(
      ProcessFileCommand command,
      CancellationToken cancellationToken)
  {
    // 1. Get file and metadata
    var file = await fileRepository.GetByIdAsync(command.FileId, cancellationToken);
    if (file is null)
      return Result.NotFound($"File with ID {command.FileId.Value} not found");

    var metadataSpec = new FileMetadataByFileIdSpec(file.Id);
    var metadata = await metadataRepository.FirstOrDefaultAsync(metadataSpec, cancellationToken);
    if (metadata is null)
      return Result.NotFound($"FileMetadata for file {command.FileId.Value} not found");

    // 2. Validate status
    if (metadata.Status != FileProcessingStatus.Queued &&
        metadata.Status != FileProcessingStatus.Pending)
    {
      logger.LogWarning(
          "File {FileId} is in status {Status}, skipping processing",
          file.Id.Value, metadata.Status.Name);

      return Result.Invalid(new ValidationError(
          "Status",
          $"File cannot be processed from status '{metadata.Status.Name}'"));
    }

    // 3. Check if extraction is supported
    if (!textExtractionService.IsSupported(file.Extension))
    {
      metadata.FailProcessing($"File extension '{file.Extension}' is not supported for text extraction");
      await metadataRepository.UpdateAsync(metadata, cancellationToken);

      return new ProcessFileResult(
          file.Id.Value,
          Success: false,
          ExtractedText: null,
          WordCount: null,
          PageCount: null,
          ErrorMessage: $"Unsupported extension: {file.Extension}");
    }

    // 4. Start processing
    metadata.StartProcessing();
    await metadataRepository.UpdateAsync(metadata, cancellationToken);

    try
    {
      // 5. Get storage container
      var storage = await storageRepository.GetByIdAsync(file.StorageId, cancellationToken);
      if (storage is null)
      {
        metadata.FailProcessing("Storage not found");
        await metadataRepository.UpdateAsync(metadata, cancellationToken);
        return Result.NotFound($"Storage with ID {file.StorageId.Value} not found");
      }

      // 6. Download file from blob storage
      logger.LogInformation(
          "Downloading file {FileId} from blob path {BlobPath}",
          file.Id.Value, file.BlobPath);

      var downloadResult = await blobStorageService.DownloadFileAsync(
          storage.ContainerName,
          file.BlobPath,
          cancellationToken);

      if (!downloadResult.IsSuccess)
      {
        metadata.FailProcessing($"Failed to download file: {downloadResult.Errors}");
        await metadataRepository.UpdateAsync(metadata, cancellationToken);
        return Result.Error($"Failed to download file: {downloadResult.Errors}");
      }

      // 7. Extract text
      await using var fileStream = downloadResult.Value;
      var extractionResult = await textExtractionService.ExtractTextAsync(
          fileStream,
          file.OriginalFileName.Value,
          file.ContentType,
          cancellationToken);

      // 8. Update metadata
      if (extractionResult.IsSuccess)
      {
        metadata.CompleteProcessing(
            extractedText: extractionResult.ExtractedText,
            pageCount: extractionResult.PageCount,
            wordCount: extractionResult.WordCount,
            detectedLanguage: extractionResult.DetectedLanguage,
            contentHash: extractionResult.ContentHash);

        if (extractionResult.AdditionalMetadata != null)
        {
          metadata.SetAdditionalMetadata(
              System.Text.Json.JsonSerializer.Serialize(extractionResult.AdditionalMetadata));
        }

        logger.LogInformation(
            "File {FileId} processed successfully. WordCount: {WordCount}, PageCount: {PageCount}",
            file.Id.Value, extractionResult.WordCount, extractionResult.PageCount);
      }
      else
      {
        metadata.FailProcessing(extractionResult.ErrorMessage ?? "Unknown error");
        logger.LogWarning(
            "File {FileId} processing failed: {Error}",
            file.Id.Value, extractionResult.ErrorMessage);
      }

      await metadataRepository.UpdateAsync(metadata, cancellationToken);

      return new ProcessFileResult(
          file.Id.Value,
          extractionResult.IsSuccess,
          extractionResult.ExtractedText,
          extractionResult.WordCount,
          extractionResult.PageCount,
          extractionResult.ErrorMessage);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error processing file {FileId}", file.Id.Value);

      metadata.FailProcessing($"Unexpected error: {ex.Message}");
      await metadataRepository.UpdateAsync(metadata, cancellationToken);

      return Result.Error($"Processing failed: {ex.Message}");
    }
  }
}
