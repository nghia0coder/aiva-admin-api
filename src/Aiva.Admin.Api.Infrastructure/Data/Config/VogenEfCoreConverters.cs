using Vogen;

namespace Aiva.Admin.Api.Infrastructure.Data.Config;

using Core.ContributorAggregate;
using Core.StorageAggregate;

[EfCoreConverter<ContributorId>]
[EfCoreConverter<ContributorName>]

[EfCoreConverter<StorageId>]
[EfCoreConverter<StorageName>]
[EfCoreConverter<StorageDescription>]
internal partial class VogenEfCoreConverters;
