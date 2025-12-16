namespace Aiva.Admin.Api.UseCases.Files.EmbedFile;

using Core.Commons.Results;
using Core.FileAggregate;

public sealed record EmbedFileCommand(FileId FileId) : ICommand<Result<EmbedFileResult>>;
