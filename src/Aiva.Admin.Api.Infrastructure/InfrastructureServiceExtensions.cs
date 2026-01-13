namespace Aiva.Admin.Api.Infrastructure;

using Aiva.Admin.Api.Infrastructure.SystemPrompts;
using AzureAI;
using BlobStorage;
using Configuration;
using Core.Interfaces;
using Core.Services;
using Data;
using Data.Config;
using Data.Queries;
using Data.Seeding;
using Embedding;
using Microsoft.Extensions.Caching.Memory;
using Retrieval;
using TextExtraction;
using TextExtraction.Extractors;
using UseCases.Contributors.List;
using UseCases.Folders.GetByStorage;
using UseCases.Storages.List;
using VectorStore;
using VectorStore.AzureAISearch;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    AppSettings appSettings,
    ConfigurationManager config,
    ILogger logger)
  {
    // Try to get connection strings in order of priority:
    // 1. "cleanarchitecture" - provided by Aspire when using .WithReference(cleanArchDb)
    // 2. "DefaultConnection" - traditional SQL Server connection
    // 3. "SqliteConnection" - fallback to SQLite
    string? connectionString = config.GetConnectionString(appSettings.ConnectionStrings.AivaChatbotDb)
                               ?? appSettings.ConnectionStrings.DefaultConnection
                               ?? appSettings.ConnectionStrings.SqliteConnection;
    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();

      // Use SQL Server if Aspire or DefaultConnection is available, otherwise use SQLite
      if (config.GetConnectionString(appSettings.ConnectionStrings.AivaChatbotDb) != null ||
          appSettings.ConnectionStrings.DefaultConnection != null)
      {
        options.UseSqlServer(connectionString);
      }
      else
      {
        options.UseSqlite(connectionString);
      }

      options.AddInterceptors(eventDispatchInterceptor);
    });

    services.AddSingleton<IMemoryCache, MemoryCache>();
    services.AddScoped<ISystemPromptService, SystemPromptService>();

    // Register Retrieval Service
    services.AddScoped<IRetrievalService, RetrievalService>();

    // Configure Embedding Service
    services.Configure<EmbeddingConfiguration>(
        config.GetSection(EmbeddingConfiguration.SectionName));
    services.AddSingleton<IEmbeddingService, AzureOpenAIEmbeddingService>();

    // Configure Chunking Service
    services.AddSingleton<IChunkingService, SemanticTextChunker>();

    // Configure Vector Store
    var vectorStoreConfig = config.GetSection(VectorStoreConfiguration.SectionName)
        .Get<VectorStoreConfiguration>() ?? new VectorStoreConfiguration();

    services.Configure<AzureAISearchConfiguration>(
        config.GetSection(AzureAISearchConfiguration.SectionName));
    services.AddSingleton<IVectorStoreService, AzureAISearchVectorStoreService>();
    logger.LogInformation("Using Azure AI Search as vector store");

    // Register IVectorStoreSettings interface
    services.Configure<VectorStoreConfiguration>(
        config.GetSection(VectorStoreConfiguration.SectionName));
    services.AddSingleton<IVectorStoreSettings>(sp =>
        sp.GetRequiredService<IOptions<VectorStoreConfiguration>>().Value);

    // Configure Text Extraction
    services.Configure<TextExtractionConfiguration>(
        config.GetSection(TextExtractionConfiguration.SectionName));

    // Register extractors (Strategy Pattern)
    services.AddSingleton<ITextExtractor, PdfTextExtractor>();
    services.AddSingleton<ITextExtractor, WordTextExtractor>();
    services.AddSingleton<ITextExtractor, ExcelTextExtractor>();
    services.AddSingleton<ITextExtractor, PowerPointTextExtractor>();
    services.AddSingleton<ITextExtractor, PlainTextExtractor>();
    services.AddSingleton<ITextExtractor, ImageTextExtractor>();

    // Register main extraction service
    services.AddSingleton<ITextExtractionService, TextExtractionService>();

    // Configure Azure AI / OpenAI
    services.Configure<AzureAIConfiguration>(
            config.GetSection(AzureAIConfiguration.SectionName));

    services.AddSingleton<IChatCompletionService, AzureOpenAIChatService>();
    services.AddSingleton<ITitleGenerationService, AzureOpenAITitleGenerationService>();

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
