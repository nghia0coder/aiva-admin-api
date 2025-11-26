using System.Net;
using System.Net.Http.Json;
using Aiva.Admin.Api.Infrastructure.Data.Seeding;
using Aiva.Admin.Api.Web.Storages.Create;
using Aiva.Admin.Api.Web.Storages.List;

namespace Aiva.Admin.Api.FunctionalTests.ApiEndpoints.Storages;

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
      StorageName = "New Test Storage",
      StorageDescription = "Test Description"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/Storages", request);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.Created);
  }

  [Fact]
  public async Task CreatedStorageAppearsInList()
  {
    // Arrange
    var uniqueName = $"Unique Storage {Guid.NewGuid():N}";
    var request = new CreateStorageRequest()
    {
      StorageName = uniqueName,
      StorageDescription = "Description"
    };

    // Act - Create the storage
    var createResponse = await _client.PostAsJsonAsync("/Storages", request);
    createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

    // Assert - Verify it appears in the list
    var listResult = await _client.GetAndDeserializeAsync<ListStorageResponse>("/Storages");
    listResult.Items.ShouldContain(s => s.StorageName == uniqueName);
  }

  [Fact]
  public async Task ReturnsCreatedStorageWithCorrectId()
  {
    // Arrange
    var request = new CreateStorageRequest()
    {
      StorageName = "Storage With Valid Id",
      StorageDescription = "Description"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/Storages", request);
    var result = await response.Content.ReadFromJsonAsync<CreateStorageResponse>();

    // Assert
    result.ShouldNotBeNull();
    result.Id.ShouldBeGreaterThan(StorageSeeder.NUMBER_OF_STORAGES); // ID should be after seed data
  }

  [Fact]
  public async Task IncreasesTotalCountAfterCreate()
  {
    // Arrange - Get initial count
    var beforeResult = await _client.GetAndDeserializeAsync<ListStorageResponse>("/Storages");
    var initialCount = beforeResult.TotalCount;

    var request = new CreateStorageRequest()
    {
      StorageName = $"Storage {Guid.NewGuid():N}",
      StorageDescription = "Description"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/Storages", request);
    response.StatusCode.ShouldBe(HttpStatusCode.Created);

    // Assert - Count increased by 1
    var afterResult = await _client.GetAndDeserializeAsync<ListStorageResponse>("/Storages");
    afterResult.TotalCount.ShouldBe(initialCount + 1);
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

  [Fact]
  public async Task ReturnsBadRequestForNullName()
  {
    // Arrange
    var request = new CreateStorageRequest()
    {
      StorageName = null!,
      StorageDescription = "Description"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/Storages", request);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }
}
