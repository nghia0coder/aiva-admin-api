using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace Aiva.Admin.Api.Web.Configurations;

using Microsoft.Extensions.Logging;

public static class AuthConfig
{
  public static IServiceCollection AddAuthConfig(
      this IServiceCollection services,
      IConfiguration configuration,
      ILogger logger)
  {
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(configuration.GetSection("AppSettings:AzureAd"));

    services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
    {
      options.Events = new JwtBearerEvents
      {
        OnMessageReceived = context =>
        {
          var accessToken = context.Request.Query["access_token"];

          // If the request is for our SignalR hub...
          var path = context.HttpContext.Request.Path;
          if (!string.IsNullOrEmpty(accessToken) &&
              (path.StartsWithSegments("/hubs/conversation")))
          {
            // Read the token out of the query string
            context.Token = accessToken;
          }
          return Task.CompletedTask;
        }
      };
    });

    services.AddAuthorization();

    logger.LogInformation("Azure AD authentication configured");

    return services;
  }
}
