namespace Aiva.Admin.Api.UseCases.Files.Delete;

using Ardalis.Result;
using Core.FileAggregate;

/// <summary>
/// Command to delete a file from blob storage, Azure AI Search, and database
/// </summary>
public sealed record DeleteFileCommand(FileId FileId) : ICommand<Result>;
