using Aiva.Admin.Api.AspireTests.Collections;

namespace Aiva.Admin.Api.AspireTests;

/// <summary>
/// Tests that verify all Aspire-managed resources are healthy and properly configured.
/// These tests ensure the distributed application infrastructure is working correctly.
/// </summary>
[Collection(AspireTestCollection.Name)]
public class ResourceHealthTests(AspireAppFixture fixture)
{
  private readonly AspireAppFixture _fixture = fixture;

  /// <summary>
  /// Resource names as defined in the AspireHost Program.cs
  /// </summary>
  private static class ResourceNames
  {
    public const string SqlServer = "sqlserver";
    public const string Database = "aiva-chatbot-db";
    public const string Papercut = "papercut";
    public const string WebApi = "web";
  }

  [Fact]
  public async Task SqlServer_ShouldBeRunning()
  {
    // Act
    await _fixture.WaitForResourceAsync(ResourceNames.SqlServer);

    // Assert - If we get here without timeout, SQL Server is running
    Assert.True(true);
  }

  [Fact]
  public async Task Database_ShouldBeRunning()
  {
    // Act
    await _fixture.WaitForResourceAsync(ResourceNames.Database);

    // Assert - If we get here without timeout, database is running
    Assert.True(true);
  }

  [Fact]
  public async Task Papercut_ShouldBeRunning()
  {
    // Act
    await _fixture.WaitForResourceAsync(ResourceNames.Papercut);

    // Assert - If we get here without timeout, Papercut is running
    Assert.True(true);
  }

  [Fact]
  public async Task WebApi_ShouldBeRunning()
  {
    // Act
    await _fixture.WaitForResourceAsync(ResourceNames.WebApi);

    // Assert - If we get here without timeout, Web API is running
    Assert.True(true);
  }

  [Fact]
  public async Task AllResources_ShouldBeRunning()
  {
    // Act - Wait for all resources in parallel
    await _fixture.WaitForResourcesAsync(
      ResourceNames.SqlServer,
      ResourceNames.Database,
      ResourceNames.Papercut,
      ResourceNames.WebApi
    );

    // Assert - If we get here without timeout, all resources are running
    Assert.True(true);
  }

  [Fact]
  public async Task WebApi_HealthEndpoint_ShouldReturnHealthy()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(ResourceNames.WebApi);
    var httpClient = _fixture.CreateHttpClient(ResourceNames.WebApi);

    // Act
    var response = await httpClient.GetAsync("/health");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  [Fact]
  public async Task WebApi_AliveEndpoint_ShouldReturnOk()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(ResourceNames.WebApi);
    var httpClient = _fixture.CreateHttpClient(ResourceNames.WebApi);

    // Act
    var response = await httpClient.GetAsync("/alive");

    // Assert
    response.StatusCode.ShouldBe(HttpStatusCode.OK);
  }

  [Fact]
  public async Task Database_ConnectionString_ShouldBeAvailable()
  {
    // Arrange
    await _fixture.WaitForResourceAsync(ResourceNames.Database);

    // Act
    var connectionString = await _fixture.GetConnectionStringAsync(ResourceNames.Database);

    // Assert
    connectionString.ShouldNotBeNullOrEmpty();
    connectionString.ShouldContain("aiva-chatbot-db");
  }
}

