using Aiva.Admin.Api.Infrastructure;
using Aiva.Admin.Api.Infrastructure.Configuration;
using Aiva.Admin.Api.UseCases.Files.ProcessFile;
using Aiva.Admin.Api.Worker.Services;
using Serilog;
using Serilog.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Logging.AddSerilog(logger);

var startupLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

startupLogger.LogInformation("Starting Aiva Admin Worker");

builder.AddServiceDefaults();

startupLogger.LogInformation("Configuring AppSettings");

// Configure AppSettings
var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);
var appSettings = appSettingsSection.Get<AppSettings>()
    ?? throw new InvalidOperationException(
        "AppSettings configuration section is missing or invalid. " +
        "Please ensure 'AppSettings' section exists in appsettings.json");
builder.Services.AddSingleton(appSettings);

builder.Services.AddInfrastructureServices(builder.Configuration, startupLogger);

builder.Services.AddMediator(options =>
{
  options.ServiceLifetime = ServiceLifetime.Scoped;
  options.Assemblies =
  [
    typeof(ProcessFileCommand),                // UseCases
    typeof(InfrastructureServiceExtensions),   // Infrastructure
    typeof(Program)                            // Worker
  ];
});

builder.Services.AddHostedService<FileProcessingBackgroundService>();

var host = builder.Build();

startupLogger.LogInformation("Worker host built successfully");

await host.RunAsync();
