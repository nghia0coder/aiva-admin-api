namespace Aiva.Admin.Api.Web.Configurations;

using Microsoft.Extensions.Logging;

public static class CorsConfigs
{
  public const string DefaultPolicyName = "AivaAdminCorsPolicy";

  public static IServiceCollection AddCorsConfig(
      this IServiceCollection services,
      IConfiguration configuration,
      IHostEnvironment environment,
      ILogger logger)
  {
    // Get CORS settings from AppSettings section
    var appSettingsSection = configuration.GetSection("AppSettings");
    var corsSettings = appSettingsSection
        .GetSection("Cors")
        .Get<Infrastructure.Configuration.CorsSettings>() ?? new Infrastructure.Configuration.CorsSettings();

    services.AddCors(options =>
    {
      options.AddPolicy(DefaultPolicyName, builder =>
      {
        if (environment.IsDevelopment())
        {
          // More permissive in development
          builder.SetIsOriginAllowed(_ => true)
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
        }
        else
        {
          // Production: strict origin checking
          if (corsSettings.AllowedOrigins.Length > 0)
          {
            builder.WithOrigins(corsSettings.AllowedOrigins);
          }

          builder.WithMethods(corsSettings.AllowedMethods)
                 .WithHeaders(corsSettings.AllowedHeaders)
                 .SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.MaxAgeSeconds));

          if (corsSettings.ExposedHeaders.Length > 0)
          {
            builder.WithExposedHeaders(corsSettings.ExposedHeaders);
          }

          if (corsSettings.AllowCredentials)
          {
            builder.AllowCredentials();
          }
          else
          {
            builder.DisallowCredentials();
          }
        }
      });
    });

    logger.LogInformation("CORS policy '{PolicyName}' configured with {OriginCount} allowed origins",
        DefaultPolicyName, corsSettings.AllowedOrigins.Length);

    return services;
  }
}
