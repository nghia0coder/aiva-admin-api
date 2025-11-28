namespace Aiva.Admin.Api.Web.Storages.GetFolders;

public record GetFoldersRequest(int StorageId, bool AsTree = true);
