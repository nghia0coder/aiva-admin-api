namespace Aiva.Admin.Api.UseCases.Files.GetStatus;

using Core.FileAggregate;

public record GetFileStatusQuery(FileId FileId) : IQuery<Result<FileStatusDTO>>;
