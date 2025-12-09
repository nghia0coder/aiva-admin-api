using Aiva.Admin.Api.Infrastructure;
using Aiva.Admin.Api.UseCases.Files.ProcessFile;
using Aiva.Admin.Api.Worker;
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

builder.Services.Configure<WorkerConfiguration>(
    builder.Configuration.GetSection(WorkerConfiguration.SectionName));

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
