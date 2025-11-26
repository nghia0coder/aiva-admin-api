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
    e.Port = 25;         // host port (standard SMTP)
    e.Protocol = ProtocolType.Tcp;
    e.UriScheme = "smtp";
  })
  .WithEndpoint("ui", e =>
  {
    e.TargetPort = 37408;
    e.Port = 37408;      // Fixed port for Papercut web UI
    e.UriScheme = "http";
  });

// Add the web project with the database connection
builder.AddProject<Projects.Aiva_Admin_Api_Web>("web")
  .WithReference(aivaChatbotDb)
  .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
  .WithEnvironment("Papercut__Smtp__Url", papercut.GetEndpoint("smtp"))
  .WaitFor(aivaChatbotDb)
  .WaitFor(papercut);

builder
  .Build()
  .Run();
