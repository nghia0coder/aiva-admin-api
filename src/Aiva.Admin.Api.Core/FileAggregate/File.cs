using Ardalis.GuardClauses;

namespace Aiva.Admin.Api.Core.FileAggregate;

using Core.FolderAggregate;
using Core.StorageAggregate;

public class File : AuditableEntity<File, FileId>, IAggregateRoot
{
  public FileName OriginalFileName { get; private set; }
  public string StoredFileName { get; private set; } = string.Empty;
  public string Extension { get; private set; } = string.Empty;
  public string ContentType { get; private set; } = string.Empty;
  public long FileSizeBytes { get; private set; }

  /// <summary>
  /// Full blob path: {storageName}/{folderPath}/{fileName}
  /// </summary>
  public string BlobPath { get; private set; } = string.Empty;

  /// <summary>
  /// Azure Blob URL
  /// </summary>
  public string BlobUrl { get; private set; } = string.Empty;

  /// <summary>
  /// Reference to parent Storage
  /// </summary>
  public StorageId StorageId { get; private set; }

  /// <summary>
  /// Reference to parent Folder
  /// </summary>
  public FolderId FolderId { get; private set; }

  // Required by EF Core
  private File() { }

  private File(
      FileName originalFileName,
      string extension,
      string contentType,
      long fileSizeBytes,
      StorageId storageId,
      FolderId folderId)
  {
    OriginalFileName = originalFileName;
    Extension = extension;
    ContentType = contentType;
    FileSizeBytes = fileSizeBytes;
    StorageId = storageId;
    FolderId = folderId;
  }

  public static File Create(
      FileName originalFileName,
      string extension,
      string contentType,
      long fileSizeBytes,
      StorageId storageId,
      FolderId folderId)
  {
    Guard.Against.NullOrWhiteSpace(extension);

    if (!AllowedFileExtensions.IsAllowed(extension))
      throw new ArgumentException($"File extension '{extension}' is not allowed.");

    return new File(
        originalFileName,
        extension.ToLowerInvariant(),
        contentType,
        fileSizeBytes,
        storageId,
        folderId);
  }

  /// <summary>
  /// Updates blob details when file is overwritten
  /// </summary>
  public void Update(
      string contentType,
      long fileSizeBytes,
      string storedFileName,
      string blobPath,
      string blobUrl)
  {
    ContentType = contentType;
    FileSizeBytes = fileSizeBytes;
    StoredFileName = storedFileName;
    BlobPath = blobPath;
    BlobUrl = blobUrl;
  }

  /// <summary>
  /// Sets blob storage details after successful upload
  /// </summary>
  public void SetBlobDetails(string storedFileName, string blobPath, string blobUrl)
  {
    StoredFileName = Guard.Against.NullOrWhiteSpace(storedFileName);
    BlobPath = Guard.Against.NullOrWhiteSpace(blobPath);
    BlobUrl = Guard.Against.NullOrWhiteSpace(blobUrl);
  }
}
