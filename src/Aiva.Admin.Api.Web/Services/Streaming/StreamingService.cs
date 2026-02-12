using System.Text.Json;

namespace Aiva.Admin.Api.Web.Services.Streaming;

public interface IStreamingService
{
    Task SendEventAsync<T>(HttpContext context, string eventType, T data, CancellationToken cancellationToken);
    void ConfigureResponse(HttpContext context);
}

public class StreamingService : IStreamingService
{
    public void ConfigureResponse(HttpContext context)
    {
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";
    }

    public async Task SendEventAsync<T>(HttpContext context, string eventType, T data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });
        
        await context.Response.WriteAsync($"event: {eventType}\n", cancellationToken);
        await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
}
