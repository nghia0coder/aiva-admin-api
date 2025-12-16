namespace Aiva.Admin.Api.Web.Configurations;

using Microsoft.Extensions.Logging;

public class CorsSettings
{
  public const string SectionName = "Cors";
  public string[] AllowedOrigins { get; set; } = [];
  public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"];
  public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization", "X-Requested-With"];
  public string[] ExposedHeaders { get; set; } = [];
  public bool AllowCredentials { get; set; } = false;
  public int MaxAgeSeconds { get; set; } = 600;
}

public static class CorsConfigs
{
  public const string DefaultPolicyName = "AivaAdminCorsPolicy";

  public static IServiceCollection AddCorsConfig(
      this IServiceCollection services,
      IConfiguration configuration,
      IHostEnvironment environment,
      ILogger logger)
  {
    var corsSettings = configuration
        .GetSection(CorsSettings.SectionName)
        .Get<CorsSettings>() ?? new CorsSettings();

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
