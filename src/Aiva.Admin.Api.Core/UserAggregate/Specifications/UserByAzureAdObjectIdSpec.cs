namespace Aiva.Admin.Api.Core.UserAggregate.Specifications;

public sealed class UserByAzureAdObjectIdSpec : SingleResultSpecification<User>
{
  public UserByAzureAdObjectIdSpec(AzureAdObjectId azureAdObjectId)
  {
    Query.Where(u => u.AzureAdObjectId == azureAdObjectId);
  }
}
