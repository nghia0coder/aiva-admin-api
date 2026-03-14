using Aiva.Admin.Api.Core.ContributorAggregate;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Infrastructure;
using Aiva.Admin.Api.Infrastructure.Configuration;
using Aiva.Admin.Api.UseCases.Files.ProcessFile;
using Aiva.Admin.Function.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using IEmailSender = Aiva.Admin.Api.Core.Interfaces.IEmailSender;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

// Configure Serilog
builder.Logging.ClearProviders();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog(logger);

var startupLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

startupLogger.LogInformation("Starting Aiva Admin Function App");

// Configure AppSettings
startupLogger.LogInformation("Configuring AppSettings");

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);
var appSettings = appSettingsSection.Get<AppSettings>()
    ?? throw new InvalidOperationException(
        "AppSettings configuration section is missing or invalid. " +
        "Please ensure 'AppSettings' section exists in appsettings.json");
builder.Services.AddSingleton(appSettings);

builder.Services.AddScoped<IEmailSender, Aiva.Admin.Function.Services.NoOpEmailSender>();

builder.Services.AddHttpClient<IRealtimeNotificationService, NoOpRealtimeNotificationService>(client =>
{
  // 'backend' matches the name defined in AspireHost
  client.BaseAddress = new Uri("https://backend");
});

builder.Services.AddMediator(options =>
{
  options.ServiceLifetime = ServiceLifetime.Scoped;
  options.Assemblies =
  [
    typeof(ProcessFileCommand),                // UseCases
    typeof(InfrastructureServiceExtensions),   // Infrastructure
    typeof(Contributor),                       // Core
    typeof(Program)                            // Worker
  ];
});

// Add Infrastructure Services (Database, Repositories, AI Services, etc.)
builder.Services.AddInfrastructureServices(appSettings, builder.Configuration, startupLogger);


// Add Application Insights
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

startupLogger.LogInformation("Function app configured successfully");

builder.Build().Run();
