namespace Aiva.Admin.Api.UseCases.Files.GetStatus;

using Core.FileAggregate;
using Core.FileAggregate.Specifications;

public class GetFileStatusHandler(
    IReadRepository<File> fileRepository,
    IReadRepository<FileMetadata> fileMetadataRepository)
    : IQueryHandler<GetFileStatusQuery, Result<FileStatusDTO>>
{
  public async ValueTask<Result<FileStatusDTO>> Handle(
      GetFileStatusQuery query,
      CancellationToken cancellationToken)
  {
    // Verify file exists
    var file = await fileRepository.GetByIdAsync(query.FileId, cancellationToken);
    if (file is null)
      return Result.NotFound($"File with ID {query.FileId.Value} not found.");

    // Get metadata
    var metadataSpec = new FileMetadataByFileIdSpec(query.FileId);
    var metadata = await fileMetadataRepository.FirstOrDefaultAsync(metadataSpec, cancellationToken);

    if (metadata is null)
      return Result.NotFound($"FileMetadata for File ID {query.FileId.Value} not found.");

    return new FileStatusDTO(
        file.Id.Value,
        metadata.Id.Value,
        metadata.Status.Name,
        metadata.QueuedAt,
        metadata.ProcessingStartedAt,
        metadata.ProcessingCompletedAt,
        metadata.RetryCount,
        metadata.ErrorMessage,
        metadata.PageCount,
        metadata.WordCount,
        metadata.DetectedLanguage,
        metadata.IsEmbedded,
        metadata.EmbeddedAt);
  }
}
