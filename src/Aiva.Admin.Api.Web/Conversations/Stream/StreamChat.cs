using System.Text.Json;
using Aiva.Admin.Api.UseCases.Conversations.Stream;
using Aiva.Admin.Api.Web.Services.Streaming;

namespace Aiva.Admin.Api.Web.Conversations.Stream;

public class StreamChat(
    IMediator mediator,
    IStreamingService streamingService,
    ILogger<StreamChat> logger)
    : Endpoint<StreamChatRequest>
{
  public override void Configure()
  {
    Post(StreamChatRequest.Route);
    Summary(s =>
    {
      s.Summary = "Stream AI response with Intent Routing";
      s.Description = "Enterprise-grade streaming with SQL/RAG routing based on intent.";
    });
    Tags("Conversations");
  }

  public override async Task HandleAsync(StreamChatRequest request, CancellationToken ct)
  {
    try
    {
      streamingService.ConfigureResponse(HttpContext);

      var command = new StreamChatCommand(request.ConversationId, request.Message);
      var result = await mediator.Send(command, ct);

      if (result.IsSuccess)
      {
        var response = result.Value;

        // Stream text response
        await streamingService.SendEventAsync(HttpContext, "message", new
        {
          content = response.TextResponse,
          type = "text"
        }, ct);

        // Stream markdown table if available
        if (!string.IsNullOrEmpty(response.MarkdownTable))
        {
          await streamingService.SendEventAsync(HttpContext, "table", new
          {
            content = response.MarkdownTable,
            type = "markdown"
          }, ct);
        }

        // Stream chart URL if available
        if (response.HasChart && !string.IsNullOrEmpty(response.ChartUrl))
        {
          await streamingService.SendEventAsync(HttpContext, "chart", new
          {
            url = response.ChartUrl,
            type = "highchart"
          }, ct);
        }

        // Stream chart config if available (Chart.js)
        if (response.HasChart && !string.IsNullOrEmpty(response.ChartConfig))
        {
          try
          {
            var chartConfigJson = JsonDocument.Parse(response.ChartConfig).RootElement;

            await streamingService.SendEventAsync(HttpContext, "chart", new
            {
              config = chartConfigJson,
              chartType = response.ChartType,
              type = "chartjs"
            }, ct);
          }
          catch (JsonException ex)
          {
            logger.LogError(ex, "Failed to parse chart config JSON");
            // Fallback: send as raw string
            await streamingService.SendEventAsync(HttpContext, "chart", new
            {
              config = response.ChartConfig,
              chartType = response.ChartType,
              type = "chartjs"
            }, ct);
          }
        }

        await streamingService.SendEventAsync(HttpContext, "done", new { complete = true }, ct);
      }
      else
      {
        await streamingService.SendEventAsync(HttpContext, "error", new { message = result.ValidationErrors?.FirstOrDefault() }, ct);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error in stream chat endpoint");
      await streamingService.SendEventAsync(HttpContext, "error", new { message = ex.Message }, ct);
    }
  }
}
