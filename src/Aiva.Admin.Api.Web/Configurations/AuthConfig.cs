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

    services.AddAuthorization();

    logger.LogInformation("Azure AD authentication configured");

    return services;
  }
}
