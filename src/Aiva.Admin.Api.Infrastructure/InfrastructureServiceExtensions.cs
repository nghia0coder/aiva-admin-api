namespace Aiva.Admin.Api.Infrastructure;

using Core.Interfaces;
using Core.Services;
using Data;
using Data.Queries;
using Data.Seeding;
using Infrastructure.BlobStorage;
using UseCases.Contributors.List;
using UseCases.Folders.GetByStorage;
using UseCases.Storages.List;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    // Try to get connection strings in order of priority:
    // 1. "cleanarchitecture" - provided by Aspire when using .WithReference(cleanArchDb)
    // 2. "DefaultConnection" - traditional SQL Server connection
    // 3. "SqliteConnection" - fallback to SQLite
    string? connectionString = config.GetConnectionString("aiva-chatbot-db")
                               ?? config.GetConnectionString("DefaultConnection")
                               ?? config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();

      // Use SQL Server if Aspire or DefaultConnection is available, otherwise use SQLite
      if (config.GetConnectionString("aiva-chatbot-db") != null ||
          config.GetConnectionString("DefaultConnection") != null)
      {
        options.UseSqlServer(connectionString);
      }
      else
      {
        options.UseSqlite(connectionString);
      }

      options.AddInterceptors(eventDispatchInterceptor);
    });

    // Configure Azure Blob Storage
    services.Configure<BlobStorageConfiguration>(
        config.GetSection(BlobStorageConfiguration.SectionName));
    services.AddSingleton<IBlobStorageService, BlobStorageService>();

    // Register all seeders
    services.AddScoped<IDataSeeder, ContributorSeeder>();
    services.AddScoped<IDataSeeder, StorageSeeder>();

    services.AddScoped<DatabaseSeeder>();

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
           .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>))
           .AddScoped<IGetFoldersByStorageQueryService, GetFoldersByStorageQueryService>()
           .AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
           .AddScoped<IListStoragesQueryService, ListStoragesQueryService>()
           .AddScoped<IDeleteContributorService, DeleteContributorService>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
