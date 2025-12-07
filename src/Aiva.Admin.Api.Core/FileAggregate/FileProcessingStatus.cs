namespace Aiva.Admin.Api.Core.FileAggregate;

/// <summary>
/// Processing status for file metadata extraction and indexing
/// </summary>
public sealed class FileProcessingStatus : SmartEnum<FileProcessingStatus>
{
  /// <summary>
  /// File uploaded but not yet queued for processing
  /// </summary>
  public static readonly FileProcessingStatus Pending = new(nameof(Pending), 0);

  /// <summary>
  /// File is queued and waiting to be processed
  /// </summary>
  public static readonly FileProcessingStatus Queued = new(nameof(Queued), 1);

  /// <summary>
  /// File is currently being processed (text extraction, embedding, etc.)
  /// </summary>
  public static readonly FileProcessingStatus Processing = new(nameof(Processing), 2);

  /// <summary>
  /// Processing completed successfully
  /// </summary>
  public static readonly FileProcessingStatus Completed = new(nameof(Completed), 3);

  /// <summary>
  /// Processing failed with error
  /// </summary>
  public static readonly FileProcessingStatus Failed = new(nameof(Failed), 4);

  /// <summary>
  /// Processing was cancelled
  /// </summary>
  public static readonly FileProcessingStatus Cancelled = new(nameof(Cancelled), 5);

  private FileProcessingStatus(string name, int value) : base(name, value) { }

  /// <summary>
  /// Check if this status allows reprocessing
  /// </summary>
  public bool CanReprocess => this == Failed || this == Cancelled || this == Completed;

  /// <summary>
  /// Check if processing is in progress
  /// </summary>
  public bool IsInProgress => this == Queued || this == Processing;
}
