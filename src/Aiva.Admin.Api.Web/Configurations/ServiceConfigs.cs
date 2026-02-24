
using Microsoft.AspNetCore.Authentication;

namespace Aiva.Admin.Api.Web.Configurations;

using Infrastructure.Configuration;
using Web.Services.Realtime;
using Web.Services.Streaming;
using Core.Interfaces;
using Infrastructure;
using Infrastructure.Email;
using Infrastructure.Identity;
using Microsoft.Extensions.Logging;

public static class ServiceConfigs
{
  public static IServiceCollection AddServiceConfigs(this IServiceCollection services, ILogger logger, WebApplicationBuilder builder, AppSettings appSettings)
  {
    services.AddInfrastructureServices(appSettings, builder.Configuration, logger)
            .AddMediatorSourceGen(logger)
            .AddCorsConfig(builder.Configuration, builder.Environment, logger);

    // Add Authentication
    services.AddAuthConfig(builder.Configuration, logger);

    // Add Identity Services
    services.AddHttpContextAccessor();
    services.AddMemoryCache();
    services.AddScoped<ICurrentUserService, CurrentUserService>();
    services.AddScoped<IClaimsTransformation, UserClaimsTransformation>();
    services.AddScoped<IRealtimeNotificationService, SignalRNotificationService>();
    services.AddScoped<IStreamingService, StreamingService>();

    logger.LogInformation("{Project} services registered", "Mediator Source Generator and Email Sender");

    return services;
  }
}
