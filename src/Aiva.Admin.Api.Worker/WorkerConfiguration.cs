namespace Aiva.Admin.Api.Worker;

public sealed class WorkerConfiguration
{
  public const string SectionName = "Worker";

  /// <summary>
  /// Interval between processing runs (in seconds)
  /// </summary>
  public int PollingIntervalSeconds { get; set; } = 10;

  /// <summary>
  /// Maximum number of files to process in one batch
  /// </summary>
  public int BatchSize { get; set; } = 5;

  /// <summary>
  /// Maximum concurrent file processing
  /// </summary>
  public int MaxConcurrency { get; set; } = 3;

  /// <summary>
  /// Enable/disable processing
  /// </summary>
  public bool Enabled { get; set; } = true;
}
