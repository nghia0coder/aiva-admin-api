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
      // Explicitly bypass Audience validation for local development/debugging
      options.TokenValidationParameters.ValidateAudience = false;
      options.TokenValidationParameters.ValidateIssuer = false;
      
      options.Events = new JwtBearerEvents
      {
        OnAuthenticationFailed = context => 
        {
            Console.WriteLine($"\n--- AUTHENTICATION FAILED ---\n{context.Exception.Message}\n-----------------------------\n");
            return Task.CompletedTask;
        },
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

    services.AddAuthorization(options =>
    {
      options.AddPolicy("SiuOrPersonalOnly", policy =>
      {
        policy.RequireAssertion(context =>
        {
          var email = context.User.FindFirst("preferred_username")?.Value
                   ?? context.User.FindFirst("email")?.Value
                   ?? context.User.FindFirst("unique_name")?.Value
                   ?? context.User.FindFirst("upn")?.Value
                   ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                   ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Upn)?.Value;
                   
          email = email?.ToLower();
          
          if (string.IsNullOrEmpty(email)) return false;

          return email.EndsWith("@siu.edu.vn") || email == "nghiadai.2004work@gmail.com";
        });
      });
    });

    logger.LogInformation("Azure AD authentication configured");

    return services;
  }
}
