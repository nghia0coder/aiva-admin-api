using Aiva.Admin.Api.Infrastructure.Configuration;
using Aiva.Admin.Api.Web.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults()    // This sets up OpenTelemetry logging
       .AddLoggerConfigs();     // This adds Serilog for console formatting

using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
var startupLogger = loggerFactory.CreateLogger<Program>();

startupLogger.LogInformation("Starting web host");

startupLogger.LogInformation("Configuring application settings");

// Configure AppSettings
var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);
var appSettings = appSettingsSection.Get<AppSettings>()
    ?? throw new InvalidOperationException(
        "AppSettings configuration section is missing or invalid. " +
        "Please ensure 'AppSettings' section exists in appsettings.json");

builder.Services.AddSingleton(appSettings);

startupLogger.LogInformation("Configure successful application settings");

builder.Services.AddOptionConfigs(builder.Configuration, startupLogger, builder);
builder.Services.AddServiceConfigs(startupLogger, builder);
builder.Services.AddHttpContextAccessor();

builder.Services.AddFastEndpoints()
                .SwaggerDocument(o =>
                {
                  o.ShortSchemaNames = true;
                  o.DocumentSettings = s =>
                  {
                    s.DocumentName = "v1";
                    s.Title = "Aiva Admin API";
                    s.Version = "v1";
                    s.AddAzureAdOAuth(appSettings.AzureAd);
                  };
                });

var app = builder.Build();

await app.UseAppMiddlewareAndSeedDatabase(appSettings);

app.MapDefaultEndpoints(); // Aspire health checks and metrics

app.Run();

// Make the implicit Program.cs class public, so integration tests can reference the correct assembly for host building
public partial class Program { }
