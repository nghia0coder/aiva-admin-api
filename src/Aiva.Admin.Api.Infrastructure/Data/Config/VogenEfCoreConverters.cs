using Vogen;

namespace Aiva.Admin.Api.Infrastructure.Data.Config;

using Core.ContributorAggregate;
using Core.FolderAggregate;
using Core.StorageAggregate;

[EfCoreConverter<ContributorId>]
[EfCoreConverter<ContributorName>]

[EfCoreConverter<StorageId>]
[EfCoreConverter<StorageName>]

[EfCoreConverter<FolderId>]
[EfCoreConverter<FolderName>]
internal partial class VogenEfCoreConverters;
