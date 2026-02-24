using FastEndpoints;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Aiva.Admin.Api.Web.Conversations.Chat;

public class MockStreamChatRequest
{
    public required string Message { get; set; }
    public string? UserName { get; set; }
}

public class MockStreamChatEndpoint : Endpoint<MockStreamChatRequest>
{
    private static readonly string[] MockTokens = new[]
    {
        "Based", " on", " your", " requirements", ",", " I", " recommend", " the", " following", ":",
        " Dell", " XPS", " 13", " with", " Intel", " i7", " processor", ",", " 16GB", " RAM", ",",
        " and", " 512GB", " SSD", ".", " This", " laptop", " offers", " excellent", " performance",
        " for", " programming", " while", " staying", " under", " your", " $1500", " budget", ".",
        " The", " build", " quality", " is", " outstanding", " and", " the", " display", " is",
        " perfect", " for", " long", " coding", " sessions", ".", " Would", " you", " like",
        " more", " specific", " recommendations", "?"
    };

    public override void Configure()
    {
        Post("/mock/conversations/{conversationId}/stream");
        AllowAnonymous();
        Summary(s => s.Summary = "Mock streaming chat endpoint for stress testing");
        Tags("Mock");
    }

    public override async Task HandleAsync(MockStreamChatRequest request, CancellationToken ct)
    {
        // Get ConversationId from route
        var conversationId = Route<Guid>("conversationId");

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("Message is required", ct);
            return;
        }

        // Set headers for streaming
        HttpContext.Response.ContentType = "text/event-stream";
        HttpContext.Response.Headers.Append("Cache-Control", "no-cache");
        HttpContext.Response.Headers.Append("Connection", "keep-alive");

        // Simulate DB lookup delay (conversation validation)
        await Task.Delay(Random.Shared.Next(50, 150), ct);

        // Simulate search delay (retrieving context)
        await Task.Delay(Random.Shared.Next(100, 300), ct);

        // Simulate initial AI processing delay
        await Task.Delay(Random.Shared.Next(200, 500), ct);

        // Stream tokens
        var tokenCount = Random.Shared.Next(30, 60); // Vary response length
        for (int i = 0; i < tokenCount; i++)
        {
            if (ct.IsCancellationRequested) break;

            var token = MockTokens[i % MockTokens.Length];

            // Add some variation to tokens
            if (i > 10 && Random.Shared.Next(100) < 10) // 10% chance
            {
                token = GetRandomToken(request.Message);
            }

            var eventData = $"data: {token}\n\n";
            await HttpContext.Response.WriteAsync(eventData, Encoding.UTF8, ct);
            await HttpContext.Response.Body.FlushAsync(ct);

            // Simulate realistic streaming delay between tokens
            var delay = Random.Shared.Next(20, 100); // 20-100ms between tokens
            await Task.Delay(delay, ct);
        }

        // Send completion signal
        await HttpContext.Response.WriteAsync("data: [DONE]\n\n", ct);
        await HttpContext.Response.Body.FlushAsync(ct);
    }

    private static string GetRandomToken(string userMessage)
    {
        // Generate contextual tokens based on user message keywords
        var keywords = userMessage.ToLower().Split(' ');

        if (keywords.Contains("laptop") || keywords.Contains("programming"))
            return Random.Shared.Next(2) == 0 ? " laptop" : " computer";

        if (keywords.Contains("kitchen") || keywords.Contains("appliance"))
            return Random.Shared.Next(2) == 0 ? " kitchen" : " appliance";

        if (keywords.Contains("smartphone") || keywords.Contains("phone"))
            return Random.Shared.Next(2) == 0 ? " phone" : " smartphone";

        return " item";
    }
}
