using Aiva.Admin.Api.AspireTests.Collections;
using Aiva.Admin.Api.Infrastructure.Data;
using Aiva.Admin.Api.Web.Contributors;
using Aiva.Admin.Api.Web.Contributors.Create;
using Aiva.Admin.Api.Web.Contributors.List;

namespace Aiva.Admin.Api.AspireTests.ApiEndpoints.Contributors;

/// <summary>
/// Integration tests for Contributor API endpoints using Aspire orchestration.
/// Tests the full stack including database operations through the Aspire-managed infrastructure.
/// </summary>
[Collection(AspireTestCollection.Name)]
public class ContributorEndpointTests(AspireAppFixture fixture)
{
    private readonly AspireAppFixture _fixture = fixture;
    private const string WebResourceName = "web";

    [Fact]
    public async Task ListContributors_ReturnsSeededContributors()
    {
        // Arrange
        await _fixture.WaitForResourceAsync(WebResourceName);
        var httpClient = _fixture.CreateHttpClient(WebResourceName);

        // Act
        var response = await httpClient.GetAsync("/Contributors");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ListContributorResponse>();
        result.ShouldNotBeNull();

        // Use >= to handle test isolation (other tests may add data)
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(SeedData.NUMBER_OF_CONTRIBUTORS);

        // Verify seeded data exists
        result.Items.ShouldContain(i => i.Name == SeedData.Contributor1.Name);
        result.Items.ShouldContain(i => i.Name == SeedData.Contributor2.Name);
    }

    [Fact]
    public async Task ListContributors_SupportsPagination()
    {
        // Arrange
        await _fixture.WaitForResourceAsync(WebResourceName);
        var httpClient = _fixture.CreateHttpClient(WebResourceName);
        const int pageSize = 1;

        // Act
        var response = await httpClient.GetAsync($"/Contributors?page=1&per_page={pageSize}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ListContributorResponse>();
        result.ShouldNotBeNull();
        result.Items.Count.ShouldBeLessThanOrEqualTo(pageSize);

        // Use >= to handle test isolation (other tests may add data)
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(SeedData.NUMBER_OF_CONTRIBUTORS);
    }

    [Fact]
    public async Task ListContributors_ReturnsLinkHeader_WhenMultiplePages()
    {
        // Arrange
        await _fixture.WaitForResourceAsync(WebResourceName);
        var httpClient = _fixture.CreateHttpClient(WebResourceName);
        const int pageSize = 1;

        // Act
        var response = await httpClient.GetAsync($"/Contributors?page=1&per_page={pageSize}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ListContributorResponse>();
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
    public async Task GetContributorById_ReturnsContributor_WhenExists()
    {
        // Arrange
        await _fixture.WaitForResourceAsync(WebResourceName);
        var httpClient = _fixture.CreateHttpClient(WebResourceName);

        // First get the list to find an existing ID
        var listResponse = await httpClient.GetFromJsonAsync<ListContributorResponse>("/Contributors");
        listResponse.ShouldNotBeNull();
        listResponse.Items.ShouldNotBeEmpty();

        var existingContributor = listResponse.Items.First();

        // Act
        var response = await httpClient.GetAsync($"/Contributors/{existingContributor.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ContributorRecord>();
        result.ShouldNotBeNull();
        result.Id.ShouldBe(existingContributor.Id);
        result.Name.ShouldBe(existingContributor.Name);
    }

    [Fact]
    public async Task GetContributorById_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        await _fixture.WaitForResourceAsync(WebResourceName);
        var httpClient = _fixture.CreateHttpClient(WebResourceName);
        const int nonExistentId = 99999;

        // Act
        var response = await httpClient.GetAsync($"/Contributors/{nonExistentId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateContributor_ReturnsCreated_WhenValidRequest()
    {
        // Arrange
        await _fixture.WaitForResourceAsync(WebResourceName);
        var httpClient = _fixture.CreateHttpClient(WebResourceName);
        var uniqueName = $"Test Contributor {Guid.NewGuid():N}";

        var request = new CreateContributorRequest
        {
            Name = uniqueName,
            PhoneNumber = "+1234567890"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/Contributors", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateContributorResponse>();
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);

        // Verify location header
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldContain($"/Contributors/{result.Id}");
    }

    [Fact]
    public async Task CreateContributor_ReturnsBadRequest_WhenInvalidRequest()
    {
        // Arrange
        await _fixture.WaitForResourceAsync(WebResourceName);
        var httpClient = _fixture.CreateHttpClient(WebResourceName);

        var request = new CreateContributorRequest
        {
            Name = "", // Invalid: empty name
            PhoneNumber = "+1234567890"
        };

        // Act
        var response = await httpClient.PostAsJsonAsync("/Contributors", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}

