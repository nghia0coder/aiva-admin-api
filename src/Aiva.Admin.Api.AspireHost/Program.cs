using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var webApi = builder.AddProject<Projects.Aiva_Admin_Api_Web>("backend")
  .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);


if (builder.Environment.IsDevelopment())
{
  var frontend = builder.AddNpmApp("frontend", @"D:\Aiva\aiva-admin-fe", "start")
    .WithReference(webApi)
    .WithHttpEndpoint(port: 4200, env: "PORT", isProxied: false)
    .WithExternalHttpEndpoints()
    .WaitFor(webApi);

  // Add the worker project for background processing
  builder.AddProject<Projects.Aiva_Admin_Api_Worker>("worker")
  .WithReference(webApi)
  .WithEnvironment("DOTNET_ENVIRONMENT", builder.Environment.EnvironmentName)
  .WaitFor(webApi); // Worker waits for Web to ensure DB is migrated
}

builder.AddAzureFunctionsProject<Projects.Aiva_Admin_Function>("aiva-admin-function");

builder
  .Build()
  .Run();
