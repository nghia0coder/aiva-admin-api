using Aiva.Admin.Api.Infrastructure.Configuration;
using NSwag;
using NSwag.Generation.AspNetCore;

public static class SwaggerAuthExtensions
{
  public static void AddAzureAdOAuth(this AspNetCoreOpenApiDocumentGeneratorSettings settings, AzureAdSettings azureAdSettings)
  {
    var tenantId = azureAdSettings.TenantId;
    var apiClientId = azureAdSettings.ClientId;
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(apiClientId))
      return;

    var scope = $"api://{apiClientId}/.default";

    settings.AddAuth("OAuth2", new OpenApiSecurityScheme
    {
      Type = OpenApiSecuritySchemeType.OAuth2,
      Flows = new OpenApiOAuthFlows
      {
        AuthorizationCode = new OpenApiOAuthFlow
        {
          AuthorizationUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize",
          TokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token",
        }
      },
    });
  }
}
