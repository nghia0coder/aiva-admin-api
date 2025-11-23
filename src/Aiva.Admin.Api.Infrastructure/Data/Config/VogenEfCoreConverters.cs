using Aiva.Admin.Api.Core.ContributorAggregate;
using Vogen;

namespace Aiva.Admin.Api.Infrastructure.Data.Config;

[EfCoreConverter<ContributorId>]
[EfCoreConverter<ContributorName>]
internal partial class VogenEfCoreConverters;
