namespace Aiva.Admin.Api.Web.Conversations.Chat;

public record ChatResponse(
    Guid MessageId,
    string Role,
    string Content,
    DateTime CreatedAt,
    string ResponseType = "text",
    TableDataResponse? StructuredData = null);

// Structured response models for Web API
public record TableDataResponse(
    TableMetadataResponse Metadata,
    IReadOnlyList<TableColumnResponse> Columns,
    IReadOnlyList<TableRowResponse> Rows,
    IReadOnlyList<ActionMetadataResponse> GlobalActions);

public record TableMetadataResponse(
    string Title,
    string? Description,
    int TotalCount,
    int DisplayedCount);

public record TableColumnResponse(
    string Key,
    string Label,
    string Type,
    bool Sortable,
    bool Filterable);

public record TableRowResponse(
    string Id,
    IReadOnlyDictionary<string, object?> Cells,
    IReadOnlyList<ActionMetadataResponse> Actions);

public record ActionMetadataResponse(
    string Type,
    string Label,
    string? Icon,
    string Endpoint,
    string Method,
    IReadOnlyDictionary<string, object> Params,
    bool IsDisabled,
    string? DisabledReason);
