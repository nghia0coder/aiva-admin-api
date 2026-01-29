namespace Aiva.Admin.Api.Web.Folders.GetContents;

public record GetFolderContentsResponse(
  FolderBreadcrumbRecord[] Breadcrumbs,
  FolderCardRecord[] Folders,
  FileCardRecord[] Files,
  FolderStatsRecord Stats,
  PaginationRecord Pagination
);

public record FolderBreadcrumbRecord(
  int? Id,
  string Name,
  string Path
);

public record FolderCardRecord(
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

public record FileCardRecord(
  int Id,
  string OriginalFileName,
  string Extension,
  string ContentType,
  long FileSizeBytes,
  string BlobUrl,
  string ProcessingStatus,
  DateTime CreatedOnUtc
);

public record FolderStatsRecord(
  int TotalFolders,
  int TotalFiles,
  long TotalSizeBytes
);

public record PaginationRecord(
  int Page,
  int PageSize,
  int TotalCount,
  bool HasNextPage,
  bool HasPreviousPage
);
