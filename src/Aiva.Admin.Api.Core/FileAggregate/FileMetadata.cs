using Aiva.Admin.Api.Core.FileAggregate.Events;
using Ardalis.GuardClauses;

namespace Aiva.Admin.Api.Core.FileAggregate;
/// <summary>
/// Stores processing state and extracted metadata for a file.
/// This is a 1:1 relationship with File entity.
/// </summary>
public class FileMetadata : AuditableEntity<FileMetadata, FileMetadataId>, IAggregateRoot
{
  /// <summary>
  /// Reference to the parent File
  /// </summary>
  public FileId FileId { get; private set; }

  /// <summary>
  /// Current processing status
  /// </summary>
  public FileProcessingStatus Status { get; private set; } = FileProcessingStatus.Pending;

  /// <summary>
  /// When processing was queued
  /// </summary>
  public DateTime? QueuedAt { get; private set; }

  /// <summary>
  /// When processing started
  /// </summary>
  public DateTime? ProcessingStartedAt { get; private set; }

  /// <summary>
  /// When processing completed (success or failure)
  /// </summary>
  public DateTime? ProcessingCompletedAt { get; private set; }

  /// <summary>
  /// Number of processing attempts
  /// </summary>
  public int RetryCount { get; private set; }

  /// <summary>
  /// Error message if processing failed
  /// </summary>
  public string? ErrorMessage { get; private set; }

  /// <summary>
  /// Extracted text content from the file (for searchability)
  /// </summary>
  public string? ExtractedText { get; private set; }

  /// <summary>
  /// Number of pages (for documents)
  /// </summary>
  public int? PageCount { get; private set; }

  /// <summary>
  /// Word count of extracted text
  /// </summary>
  public int? WordCount { get; private set; }

  /// <summary>
  /// Language detected in the document
  /// </summary>
  public string? DetectedLanguage { get; private set; }

  /// <summary>
  /// Hash of the file content for change detection
  /// </summary>
  public string? ContentHash { get; private set; }

  /// <summary>
  /// Whether vector embeddings have been generated
  /// </summary>
  public bool IsEmbedded { get; private set; }

  /// <summary>
  /// When embeddings were last generated
  /// </summary>
  public DateTime? EmbeddedAt { get; private set; }

  /// <summary>
  /// Additional metadata as JSON (flexible for different file types)
  /// </summary>
  public string? AdditionalMetadataJson { get; private set; }

  // Required by EF Core
  private FileMetadata() { }

  private FileMetadata(FileId fileId)
  {
    FileId = fileId;
    Status = FileProcessingStatus.Pending;
  }

  /// <summary>
  /// Creates a new FileMetadata instance for a file
  /// </summary>
  public static FileMetadata Create(FileId fileId)
  {
    Guard.Against.Default(fileId);
    return new FileMetadata(fileId);
  }

  /// <summary>
  /// Mark file as queued for processing
  /// </summary>
  public void MarkAsQueued()
  {
    if (Status.IsInProgress)
      throw new InvalidOperationException($"Cannot queue file that is already {Status.Name}");

    Status = FileProcessingStatus.Queued;
    QueuedAt = DateTime.UtcNow;
    ErrorMessage = null;
  }

  /// <summary>
  /// Mark file processing as started
  /// </summary>
  public void StartProcessing()
  {
    if (Status != FileProcessingStatus.Queued && Status != FileProcessingStatus.Pending)
      throw new InvalidOperationException($"Cannot start processing from status {Status.Name}");

    Status = FileProcessingStatus.Processing;
    ProcessingStartedAt = DateTime.UtcNow;
    RetryCount++;

    RegisterDomainEvent(new FileProcessingStartedEvent(FileId, Id));
  }

  /// <summary>
  /// Mark file processing as completed successfully
  /// </summary>
  public void CompleteProcessing(
      string? extractedText = null,
      int? pageCount = null,
      int? wordCount = null,
      string? detectedLanguage = null,
      string? contentHash = null)
  {
    if (Status != FileProcessingStatus.Processing)
      throw new InvalidOperationException($"Cannot complete processing from status {Status.Name}");

    Status = FileProcessingStatus.Completed;
    ProcessingCompletedAt = DateTime.UtcNow;
    ErrorMessage = null;

    ExtractedText = extractedText;
    PageCount = pageCount;
    WordCount = wordCount;
    DetectedLanguage = detectedLanguage;
    ContentHash = contentHash;

    RegisterDomainEvent(new FileProcessingCompletedEvent(FileId, Id, true));
  }

  /// <summary>
  /// Mark file processing as failed
  /// </summary>
  public void FailProcessing(string errorMessage)
  {
    Guard.Against.NullOrWhiteSpace(errorMessage);

    Status = FileProcessingStatus.Failed;
    ProcessingCompletedAt = DateTime.UtcNow;
    ErrorMessage = errorMessage;

    RegisterDomainEvent(new FileProcessingCompletedEvent(FileId, Id, false, errorMessage));
  }

  /// <summary>
  /// Cancel file processing
  /// </summary>
  public void CancelProcessing(string? reason = null)
  {
    Status = FileProcessingStatus.Cancelled;
    ProcessingCompletedAt = DateTime.UtcNow;
    ErrorMessage = reason ?? "Processing cancelled";
  }

  /// <summary>
  /// Reset for reprocessing
  /// </summary>
  public void ResetForReprocessing()
  {
    if (!Status.CanReprocess)
      throw new InvalidOperationException($"Cannot reprocess from status {Status.Name}");

    Status = FileProcessingStatus.Pending;
    QueuedAt = null;
    ProcessingStartedAt = null;
    ProcessingCompletedAt = null;
    ErrorMessage = null;
  }

  /// <summary>
  /// Mark embeddings as generated
  /// </summary>
  public void MarkAsEmbedded()
  {
    IsEmbedded = true;
    EmbeddedAt = DateTime.UtcNow;
  }

  /// <summary>
  /// Update additional metadata
  /// </summary>
  public void SetAdditionalMetadata(string? metadataJson)
  {
    AdditionalMetadataJson = metadataJson;
  }
}
