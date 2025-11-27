using Aiva.Admin.Api.AspireTests.Collections;
using Aiva.Admin.Api.Infrastructure.Data.Seeding;
using Aiva.Admin.Api.Web.Storages.Create;
using Aiva.Admin.Api.Web.Storages.List;

namespace Aiva.Admin.Api.AspireTests.ApiEndpoints.Storages;

/// <summary>
/// Integration tests for Storage API endpoints using Aspire orchestration.
/// Tests the full stack including database operations through the Aspire-managed infrastructure.
/// </summary>
[Collection(AspireTestCollection.Name)]
public class StorageEndpointTests(AspireAppFixture fixture)
{
  private readonly AspireAppFixture _fixture = fixture;
  private const string WebResourceName = "web";

  [Fact]
  public async Task ListStorages_ReturnsSeededStorages()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(WebResourceName);
    var httpClient = _fixture.CreateHttpClient(WebResourceName);

    // Act
    var response = await httpClient.GetAsync("/Storages");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    var result = await response.Content.ReadFromJsonAsync<ListStorageResponse>();
    result.ShouldNotBeNull();
    
    // Use >= to handle test isolation (other tests may add data)
    result.TotalCount.ShouldBeGreaterThanOrEqualTo(StorageSeeder.NUMBER_OF_STORAGES);
    
    // Verify seeded data exists
    result.Items.ShouldContain(s => s.StorageName == StorageSeeder.Storage1.StorageName.Value);
    result.Items.ShouldContain(s => s.StorageName == StorageSeeder.Storage2.StorageName.Value);
  }

  [Fact]
  public async Task ListStorages_SupportsPagination_WithCorrectPageSize()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(WebResourceName);
    var httpClient = _fixture.CreateHttpClient(WebResourceName);
    const int pageSize = 2;

    // Act
    var response = await httpClient.GetAsync($"/Storages?page=1&per_page={pageSize}");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    var result = await response.Content.ReadFromJsonAsync<ListStorageResponse>();
    result.ShouldNotBeNull();
    result.Items.Count.ShouldBeLessThanOrEqualTo(pageSize);
    
    // Use >= to handle test isolation (other tests may add data)
    result.TotalCount.ShouldBeGreaterThanOrEqualTo(StorageSeeder.NUMBER_OF_STORAGES);
    
    // Verify pagination math is correct based on actual total
    result.TotalPages.ShouldBe((int)Math.Ceiling((double)result.TotalCount / pageSize));
  }

  [Fact]
  public async Task ListStorages_ReturnsEmptyPage_WhenPageExceedsTotalPages()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(WebResourceName);
    var httpClient = _fixture.CreateHttpClient(WebResourceName);
    const int pageSize = 5;
    const int pageNumber = 100;

    // Act
    var response = await httpClient.GetAsync($"/Storages?page={pageNumber}&per_page={pageSize}");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    var result = await response.Content.ReadFromJsonAsync<ListStorageResponse>();
    result.ShouldNotBeNull();
    result.Items.ShouldBeEmpty();
    
    // Use >= to handle test isolation (other tests may add data)
    result.TotalCount.ShouldBeGreaterThanOrEqualTo(StorageSeeder.NUMBER_OF_STORAGES);
  }

  [Fact]
  public async Task ListStorages_ReturnsLinkHeader_WhenMultiplePages()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(WebResourceName);
    var httpClient = _fixture.CreateHttpClient(WebResourceName);
    const int pageSize = 1;

    // Act
    var response = await httpClient.GetAsync($"/Storages?page=1&per_page={pageSize}");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);

    var result = await response.Content.ReadFromJsonAsync<ListStorageResponse>();
    result.ShouldNotBeNull();

    // Check for Link header when there are multiple pages
    if (result.TotalCount > pageSize)
    {
      response.Headers.TryGetValues("Link", out var linkValues).ShouldBeTrue();
      var linkHeader = linkValues?.FirstOrDefault();
      linkHeader.ShouldNotBeNullOrEmpty();
      linkHeader.ShouldContain("rel=\"next\"");
    }
  }

  [Fact]
  public async Task CreateStorage_ReturnsCreated_WhenValidRequest()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(WebResourceName);
    var httpClient = _fixture.CreateHttpClient(WebResourceName);
    var uniqueName = $"Test Storage {Guid.NewGuid():N}";

    var request = new CreateStorageRequest
    {
      StorageName = uniqueName
    };

    // Act
    var response = await httpClient.PostAsJsonAsync("/Storages", request);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.Created);

    var result = await response.Content.ReadFromJsonAsync<CreateStorageResponse>();
    result.ShouldNotBeNull();
    result.Id.ShouldBeGreaterThan(0);
  }

  [Fact]
  public async Task CreateStorage_ReturnsBadRequest_WhenInvalidRequest()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(WebResourceName);
    var httpClient = _fixture.CreateHttpClient(WebResourceName);

    var request = new CreateStorageRequest
    {
      StorageName = "" // Invalid: empty name
    };

    // Act
    var response = await httpClient.PostAsJsonAsync("/Storages", request);

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
  }
}

