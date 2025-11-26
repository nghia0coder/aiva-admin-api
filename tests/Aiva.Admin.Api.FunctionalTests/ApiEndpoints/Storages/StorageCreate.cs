using System.Net;
using System.Net.Http.Json;

namespace Aiva.Admin.Api.FunctionalTests.ApiEndpoints;

using Aiva.Admin.Api.Web.Storages.Create;

[Collection("Sequential")]
public class StorageCreate(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task CreatesStorageSuccessfully()
  {
    // Arrange
    var request = new CreateStorageRequest()
    {
      StorageName = "New Storage",
      StorageDescription = "Description"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/Storages", request);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.Created);
  }

  [Fact]
  public async Task ReturnsCreatedStorageId()
  {
    // Arrange
    var request = new CreateStorageRequest()
    {
      StorageName = "Another Storage",
      StorageDescription = "Description"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/Storages", request);
    var result = await response.Content.ReadFromJsonAsync<CreateStorageResponse>();

    // Assert
    result.ShouldNotBeNull();
    result.Id.ShouldBeGreaterThan(0);
  }

  [Fact]
  public async Task ReturnsBadRequestForEmptyName()
  {
    // Arrange
    var request = new CreateStorageRequest()
    {
      StorageName = "",
      StorageDescription = "Description"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/Storages", request);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }
}
