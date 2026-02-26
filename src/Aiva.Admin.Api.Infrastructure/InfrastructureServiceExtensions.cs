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
using Formatting;
using Microsoft.Extensions.Caching.Memory;
using Retrieval;
using Services;
using TextExtraction;
using TextExtraction.Extractors;
using UseCases.Contributors.List;
using UseCases.Folders.GetByStorage;
using UseCases.Folders.GetContents;
using UseCases.Folders.List;
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
    // 2. "DefaultConnection" - traditional SQL Server connection
    // 3. "SqliteConnection" - fallback to SQLite
    string? connectionString = appSettings.ConnectionStrings.DefaultConnection ?? appSettings.ConnectionStrings.SqliteConnection;
    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<AppDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();

      if (appSettings.ConnectionStrings.DefaultConnection != null)
      {
        options.UseAzureSql(connectionString);
      }
      else
      {
        options.UseSqlite(connectionString);
      }

      options.AddInterceptors(eventDispatchInterceptor);
    });

    services.AddSingleton<IMemoryCache, MemoryCache>();

    // Register System Prompt Service based on configuration
    if (appSettings.SystemPrompt.UseFileBasedPrompts)
    {
      logger.LogInformation("Using FILE-BASED system prompts from prompts/ folder");
      services.AddScoped<ISystemPromptService, FileSystemPromptService>();
    }
    else
    {
      logger.LogInformation("Using DATABASE-BASED system prompts");
      services.AddScoped<ISystemPromptService, SystemPromptService>();
    }

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

    // Register IRetrievalSettings interface for out-of-scope detection
    services.AddSingleton<IRetrievalSettings>(appSettings.Retrieval);

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

    // Register Intent Detection Service
    services.AddScoped<IIntentDetectionService>(sp =>
    {
      var logger = sp.GetRequiredService<ILogger<AzureOpenAIIntentDetectionService>>();
      return new AzureOpenAIIntentDetectionService(
          appSettings.AzureAI,
          appSettings.IntentDetection,
          logger);
    });

    // Register Response Formatter Service
    services.AddScoped<IResponseFormatterService, ProductTableResponseFormatterService>();

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
           .AddScoped<ISqlExecutorService, SqlExecutorService>()
           .AddScoped<IGetFoldersByStorageQueryService, GetFoldersByStorageQueryService>()
           .AddScoped<IGetFolderContentsQueryService, GetFolderContentsQueryService>()
           .AddScoped<IListFoldersQueryService, ListFoldersQueryService>()
           .AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
           .AddScoped<IListStoragesQueryService, ListStoragesQueryService>()
           .AddScoped<IDeleteContributorService, DeleteContributorService>()
           .AddScoped<IConversationMessageQueryService, ConversationMessageQueryService>()
           .AddScoped<IChartGenerationService, ChartJsGenerationService>() // Changed from ChartGenerationService to ChartJsGenerationService
           .AddScoped<IPromptTemplateService, PromptTemplateService>()
           .AddScoped<IDataFormatterService, DataFormatterService>()
           .AddScoped<IChatHistoryService, ChatHistoryService>()
           .AddScoped<IJsonExtractionService, JsonExtractionService>()
           .AddScoped<IJsonParseService, JsonParseService>()
           .AddScoped<IStandaloneQuestionService, StandaloneQuestionService>()
           .AddScoped<IShoppingChatService, ShoppingChatService>()
           .AddScoped<IHtmlTableParserService, HtmlTableParserService>();


    services.AddHttpClient<ShoppingToolService>(client =>
    {
      client.BaseAddress = new Uri(appSettings.ShoppingApiConfig.BaseUrl);
      client.Timeout = TimeSpan.FromSeconds(appSettings.ShoppingApiConfig.TimeoutSeconds);
      client.DefaultRequestHeaders.Add("User-Agent", "AIVA-Shopping-Service/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
    {
      // Allow self-signed certificates for development if configured
      ServerCertificateCustomValidationCallback = appSettings.ShoppingApiConfig.AllowSelfSignedCertificates
        ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        : null
    });

    // Register IShoppingToolService separately to use the configured HttpClient
    services.AddScoped<IShoppingToolService>(sp =>
    {
      var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
      var httpClient = httpClientFactory.CreateClient(nameof(ShoppingToolService));
      var logger = sp.GetRequiredService<ILogger<ShoppingToolService>>();
      var retrievalServices = sp.GetRequiredService<IRetrievalService>();
      var retrievalSettings = sp.GetRequiredService<IRetrievalSettings>();
      return new ShoppingToolService(httpClient, retrievalServices, retrievalSettings, appSettings.ShoppingApiConfig, logger);
    });

    services.AddScoped<ISqlExecutorDbConnectionFactory, SqlExecutorDbConnectionFactory>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
