namespace Aiva.Admin.Api.UseCases.Files.ProcessFile;

using Core.FileAggregate;

public sealed record ProcessFileCommand(FileId FileId) : ICommand<Result<ProcessFileResult>>;

public sealed record ProcessFileResult(
    int FileId,
    bool Success,
    string? ExtractedText,
    int? WordCount,
    int? PageCount,
    string? ErrorMessage);
