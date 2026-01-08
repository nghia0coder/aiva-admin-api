using Ardalis.ListStartupServices;
using Scalar.AspNetCore;
using Aiva.Admin.Api.Web.Hubs;

namespace Aiva.Admin.Api.Web.Configurations;

using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Data.Seeding;

public static class MiddlewareConfig
{
  public static async Task<IApplicationBuilder> UseAppMiddlewareAndSeedDatabase(this WebApplication app, AppSettings appSettings)
  {
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseShowAllServicesMiddleware(); // see https://github.com/ardalis/AspNetCoreStartupServices
    }
    else
    {
      app.UseDefaultExceptionHandler(); // from FastEndpoints
      app.UseHsts();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseCors(CorsConfigs.DefaultPolicyName);
    app.MapHub<ConversationHub>("/hubs/conversation");
    app.UseFastEndpoints();

    if (app.Environment.IsDevelopment())
    {
      app.UseSwaggerGen(options =>
      {
        options.Path = "/openapi/{documentName}.json";
      });
      app.MapScalarApiReference(options => options
         .AddPreferredSecuritySchemes("OAuth2")
         .AddAuthorizationCodeFlow("OAuth2", flow =>
          {
            flow.ClientId = appSettings.AzureAd.ScalarClientId;
            flow.Pkce = Pkce.Sha256;
            flow.SelectedScopes = [$"api://{appSettings.AzureAd.ClientId}/.default"];
          }));
    }

    app.UseHttpsRedirection(); // Note this will drop Authorization headers

    // Run migrations and seed in Development or when explicitly requested via environment variable
    var shouldMigrate = app.Environment.IsDevelopment() ||
                        appSettings.Database.ApplyMigrationsOnStartup;

    if (shouldMigrate)
    {
      await MigrateDatabaseAsync(app);
      await SeedDatabaseAsync(app);
    }

    return app;
  }

  static async Task MigrateDatabaseAsync(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
      logger.LogInformation("Applying database migrations...");
      var context = services.GetRequiredService<AppDbContext>();
      await context.Database.MigrateAsync();
      logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "An error occurred migrating the DB. {exceptionMessage}", ex.Message);
      throw; // Re-throw to make startup fail if migrations fail
    }
  }

  static async Task SeedDatabaseAsync(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
      logger.LogInformation("Seeding database...");
      var context = services.GetRequiredService<AppDbContext>();
      var seeder = services.GetRequiredService<DatabaseSeeder>();

      await seeder.SeedAllAsync(context);

      logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "An error occurred seeding the DB. {exceptionMessage}", ex.Message);
      // Don't re-throw for seeding errors - it's not critical
    }
  }
}
