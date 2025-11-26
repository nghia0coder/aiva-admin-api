using Aiva.Admin.Api.Web.Storages.List;

namespace Aiva.Admin.Api.FunctionalTests.ApiEndpoints.Storages;

[Collection("Sequential")]
public class StorageList(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task ReturnsStoragesSuccessfully()
  {
    // Act
    var response = await _client.GetAsync("/Storages");

    // Assert
    response.EnsureSuccessStatusCode();
  }

  [Fact]
  public async Task ReturnsPagedResult()
  {
    // Act
    var result = await _client.GetAndDeserializeAsync<ListStorageResponse>("/Storages");

    // Assert
    result.ShouldNotBeNull();
    result.Items.ShouldNotBeNull();
  }

  [Fact]
  public async Task SupportsPagination()
  {
    // Act
    var result = await _client.GetAndDeserializeAsync<ListStorageResponse>("/Storages?page=1&perPage=5");

    // Assert
    result.ShouldNotBeNull();
  }
}
