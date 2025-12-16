namespace Aiva.Admin.Api.Core.Commons.Results;

public sealed record EmbedFileResult(
    int FileId,
    int ChunkCount,
    string CollectionName,
    bool Success,
    string? ErrorMessage = null);
