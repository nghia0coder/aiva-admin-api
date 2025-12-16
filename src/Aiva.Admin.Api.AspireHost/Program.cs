using System.Net.Sockets;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================================
// Port Configuration Strategy
// ============================================================================
// SQL Server: 1433 (standard SQL Server port)
// Papercut SMTP: 25 (standard SMTP port)
// Papercut UI: 37408 (custom port for email testing UI)
// Future services should follow this pattern with documented ports
// ============================================================================

// Add SQL Server container with persistent volume and fixed port
var sqlServer = builder.AddSqlServer("sqlserver", port: 1433)
  .WithLifetime(ContainerLifetime.Persistent)
  .WithVolume("sqlserver-data", "/var/opt/mssql", isReadOnly: false);

// Add the database
var aivaChatbotDb = sqlServer.AddDatabase("aiva-chatbot-db");

// Papercut SMTP container for email testing
var papercut = builder.AddContainer("papercut", "jijiechen/papercut", "latest")
  .WithEndpoint("smtp", e =>
  {
    e.TargetPort = 25;   // container port
    e.Port = 25;         // host port (standard SMTP)B
    e.Protocol = ProtocolType.Tcp;
    e.UriScheme = "smtp";
  })
  .WithEndpoint("ui", e =>
  {
    e.TargetPort = 37408;
    e.Port = 37408;      // Fixed port for Papercut web UI
    e.UriScheme = "http";
  });

// Add Qdrant vector database
var qdrant = builder.AddQdrant("qdrant")
  .WithLifetime(ContainerLifetime.Persistent)
  .WithDataVolume("qdrant-data");

var webApi = builder.AddProject<Projects.Aiva_Admin_Api_Web>("backend")
  .WithReference(aivaChatbotDb)
  .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
  .WithEnvironment("Papercut__Smtp__Url", papercut.GetEndpoint("smtp"))
  .WaitFor(aivaChatbotDb)
  .WaitFor(papercut)
  .WaitFor(qdrant);

var frontend = builder.AddNpmApp("frontend", @"D:\Aiva\aiva-admin-fe", "start")
    .WithReference(webApi)
    .WithHttpEndpoint(port: 4200, env: "PORT", isProxied: false)
    .WithExternalHttpEndpoints()
    .WaitFor(webApi);

// Add the worker project for background processing
builder.AddProject<Projects.Aiva_Admin_Api_Worker>("worker")
  .WithReference(aivaChatbotDb)
  .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
  .WaitFor(aivaChatbotDb)
  .WaitFor(webApi) // Worker waits for Web to ensure DB is migrated
  .WaitFor(qdrant);

builder
  .Build()
  .Run();
