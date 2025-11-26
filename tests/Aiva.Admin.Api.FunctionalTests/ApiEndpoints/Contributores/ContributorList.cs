using Aiva.Admin.Api.Infrastructure.Data;
using Aiva.Admin.Api.Web.Contributors.List;

namespace Aiva.Admin.Api.FunctionalTests.ApiEndpoints.Contributores;

[Collection("Sequential")]
public class ContributorList(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task ReturnsTwoContributors()
  {
    var result = await _client.GetAndDeserializeAsync<ListContributorResponse>("/Contributors");

    Assert.Equal(SeedData.NUMBER_OF_CONTRIBUTORS, result.TotalCount);
    Assert.Contains(result.Items, i => i.Name == SeedData.Contributor1.Name);
    Assert.Contains(result.Items, i => i.Name == SeedData.Contributor2.Name);
  }
}
