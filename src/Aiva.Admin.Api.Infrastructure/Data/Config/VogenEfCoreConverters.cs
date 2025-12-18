using Vogen;

namespace Aiva.Admin.Api.Infrastructure.Data.Config;

using Core.ContributorAggregate;
using Core.FileAggregate;
using Core.FolderAggregate;
using Core.StorageAggregate;
using Core.UserAggregate;

[EfCoreConverter<ContributorId>]
[EfCoreConverter<ContributorName>]

[EfCoreConverter<StorageId>]
[EfCoreConverter<StorageName>]

[EfCoreConverter<FolderId>]
[EfCoreConverter<FolderName>]

[EfCoreConverter<FileId>]
[EfCoreConverter<FileName>]
[EfCoreConverter<FileMetadataId>]

[EfCoreConverter<UserId>]
[EfCoreConverter<AzureAdObjectId>]
internal partial class VogenEfCoreConverters;
