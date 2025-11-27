namespace Aiva.Admin.Api.AspireTests.Fixtures;

/// <summary>
/// Shared fixture for Aspire integration tests.
/// Manages the lifecycle of the distributed application including SQL Server, Papercut, and Web API.
/// Implements IAsyncLifetime for proper async setup and teardown.
/// </summary>
public sealed class AspireAppFixture : IAsyncLifetime
{
  private DistributedApplication? _app;
  private ResourceNotificationService? _resourceNotificationService;

  /// <summary>
  /// The running distributed application instance.
  /// </summary>
  public DistributedApplication App => _app ?? throw new InvalidOperationException("App has not been initialized. Call InitializeAsync first.");

  /// <summary>
  /// Service for monitoring resource states.
  /// </summary>
  public ResourceNotificationService ResourceNotificationService =>
    _resourceNotificationService ?? throw new InvalidOperationException("ResourceNotificationService has not been initialized.");

  /// <summary>
  /// Timeout for waiting on resources to become ready.
  /// </summary>
  public static TimeSpan ResourceWaitTimeout => TimeSpan.FromMinutes(5);

  public async Task InitializeAsync()
  {
    // Build the Aspire app host for testing
    var appHost = await DistributedApplicationTestingBuilder
      .CreateAsync<Projects.Aiva_Admin_Api_AspireHost>();

    // Configure test-specific settings
    appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
    {
      clientBuilder.AddStandardResilienceHandler();
    });

    // Build and start the application
    _app = await appHost.BuildAsync();
    _resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();

    await _app.StartAsync();
  }

  public async Task DisposeAsync()
  {
    if (_app is not null)
    {
      await _app.StopAsync();
      await _app.DisposeAsync();
    }
  }

  /// <summary>
  /// Creates an HttpClient configured to communicate with the specified resource.
  /// </summary>
  /// <param name="resourceName">The name of the resource (e.g., "web").</param>
  /// <returns>An HttpClient instance.</returns>
  public HttpClient CreateHttpClient(string resourceName)
  {
    return App.CreateHttpClient(resourceName);
  }

  /// <summary>
  /// Waits for the specified resource to reach the running state.
  /// </summary>
  /// <param name="resourceName">The name of the resource to wait for.</param>
  /// <param name="timeout">Optional custom timeout.</param>
  public async Task WaitForResourceAsync(string resourceName, TimeSpan? timeout = null)
  {
    var waitTimeout = timeout ?? ResourceWaitTimeout;

    await ResourceNotificationService
      .WaitForResourceAsync(resourceName, KnownResourceStates.Running)
      .WaitAsync(waitTimeout);
  }

  /// <summary>
  /// Waits for multiple resources to reach the running state.
  /// </summary>
  /// <param name="resourceNames">The names of the resources to wait for.</param>
  public async Task WaitForResourcesAsync(params string[] resourceNames)
  {
    var tasks = resourceNames.Select(name => WaitForResourceAsync(name));
    await Task.WhenAll(tasks);
  }

  /// <summary>
  /// Gets the connection string for a resource.
  /// </summary>
  /// <param name="resourceName">The name of the resource.</param>
  /// <returns>The connection string if available.</returns>
  public async Task<string?> GetConnectionStringAsync(string resourceName)
  {
    return await App.GetConnectionStringAsync(resourceName);
  }
}

