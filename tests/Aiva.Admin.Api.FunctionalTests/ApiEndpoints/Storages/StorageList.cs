using Aiva.Admin.Api.Infrastructure.Data.Seeding;
using Aiva.Admin.Api.Web.Storages.List;

namespace Aiva.Admin.Api.FunctionalTests.ApiEndpoints.Storages;

[Collection("Sequential")]
public class StorageList(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task ReturnsAllSeededStorages()
  {
    // Act
    var result = await _client.GetAndDeserializeAsync<ListStorageResponse>("/Storages");

    // Assert - Verify count matches seed data
    result.TotalCount.ShouldBe(StorageSeeder.NUMBER_OF_STORAGES);
  }

  [Fact]
  public async Task ReturnsStoragesWithCorrectData()
  {
    // Act
    var result = await _client.GetAndDeserializeAsync<ListStorageResponse>("/Storages");

    // Assert - Verify seed data is present
    result.Items.ShouldContain(s => s.StorageName == StorageSeeder.Storage1.StorageName.Value);
    result.Items.ShouldContain(s => s.StorageName == StorageSeeder.Storage2.StorageName.Value);
  }

  [Fact]
  public async Task SupportsPaginationWithCorrectPageSize()
  {
    // Arrange
    const int pageSize = 2;

    // Act
    var result = await _client.GetAndDeserializeAsync<ListStorageResponse>($"/Storages?page=1&per_page={pageSize}");

    // Assert
    result.ShouldNotBeNull();
    result.Items.Count.ShouldBeLessThanOrEqualTo(pageSize);
    result.TotalCount.ShouldBe(StorageSeeder.NUMBER_OF_STORAGES);
    result.TotalPages.ShouldBe((int)Math.Ceiling((double)StorageSeeder.NUMBER_OF_STORAGES / pageSize));
  }

  [Fact]
  public async Task ReturnsEmptyPageWhenPageExceedsTotalPages()
  {
    // Arrange - Request a page beyond available data
    const int pageSize = 5;
    const int pageNumber = 100;

    // Act
    var result = await _client.GetAndDeserializeAsync<ListStorageResponse>($"/Storages?page={pageNumber}&per_page={pageSize}");

    // Assert
    result.Items.ShouldBeEmpty();
    result.TotalCount.ShouldBe(StorageSeeder.NUMBER_OF_STORAGES);
  }
}
