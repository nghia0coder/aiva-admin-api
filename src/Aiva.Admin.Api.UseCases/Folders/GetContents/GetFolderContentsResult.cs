namespace Aiva.Admin.Api.UseCases.Folders.GetContents;

using UseCases.Files;

public record GetFolderContentsResult(
  FolderBreadcrumb[] Breadcrumbs,
  FolderContentDto[] Folders,
  FileContentDto[] Files,
  FolderStats Stats,
  int TotalCount,
  int Page,
  int PageSize,
  bool HasNextPage,
  bool HasPreviousPage
);

public record FolderBreadcrumb(
  int? Id,
  string Name,
  string Path
);

public record FolderContentDto(
  int Id,
  string Name,
  string? Description,
  string BlobPrefix,
  int SubfolderCount,
  int FileCount,
  long TotalSizeBytes,
  DateTime CreatedOnUtc,
  DateTime? UpdatedOnUtc
);

public record FileContentDto(
  int Id,
  string OriginalFileName,
  string Extension,
  string ContentType,
  long FileSizeBytes,
  string BlobUrl,
  string ProcessingStatus,
  DateTime CreatedOnUtc
);

public record FolderStats(
  int TotalFolders,
  int TotalFiles,
  long TotalSizeBytes
);
