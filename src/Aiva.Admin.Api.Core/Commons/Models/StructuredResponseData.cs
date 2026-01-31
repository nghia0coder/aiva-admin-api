namespace Aiva.Admin.Api.Core.Commons.Models;

/// <summary>
/// Represents structured table data for product listings, comparisons, or other tabular information.
/// Backend provides the structure; frontend decides the rendering (table, cards, list, etc.).
/// </summary>
public sealed record TableData
{
  public required TableMetadata Metadata { get; init; }
  public required IReadOnlyList<TableColumn> Columns { get; init; }
  public required IReadOnlyList<TableRow> Rows { get; init; }
  public required IReadOnlyList<ActionMetadata> GlobalActions { get; init; }
}

/// <summary>
/// Metadata about the table content
/// </summary>
public sealed record TableMetadata(
    string Title,
    string? Description,
    int TotalCount,
    int DisplayedCount);

/// <summary>
/// Defines a column in the structured table
/// </summary>
public sealed record TableColumn(
    string Key,
    string Label,
    ColumnType Type,
    bool Sortable = true,
    bool Filterable = false);

/// <summary>
/// Column data types for appropriate rendering
/// </summary>
public enum ColumnType
{
  Text,
  Number,
  Currency,
  Image,
  Rating,
  Date,
  Boolean
}

/// <summary>
/// Represents a row in the table with cell values and available actions
/// </summary>
public sealed record TableRow(
    string Id,  // Product ID or entity identifier
    IReadOnlyDictionary<string, object?> Cells,
    IReadOnlyList<ActionMetadata> Actions);

/// <summary>
/// Metadata for action buttons/links (e.g., View Detail, Add to Cart)
/// Backend provides endpoint and params; frontend handles UI implementation
/// </summary>
public sealed record ActionMetadata(
    ActionType Type,
    string Label,
    string? Icon,
    string Endpoint,
    string Method,
    IReadOnlyDictionary<string, object> Params,
    bool IsDisabled = false,
    string? DisabledReason = null);

/// <summary>
/// Common action types for e-commerce scenarios
/// </summary>
public enum ActionType
{
  ViewDetail,
  AddToCart,
  Compare,
  Share,
  QuickView,
  AddToWishlist,
  NotifyWhenAvailable
}
