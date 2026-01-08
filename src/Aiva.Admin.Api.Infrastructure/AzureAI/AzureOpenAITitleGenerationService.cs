namespace Aiva.Admin.Api.Infrastructure.AzureAI;

using System.ClientModel;
using Ardalis.Result;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Configuration;
using Core.ConversationAggregate;
using Core.Interfaces;
using OpenAI.Chat;

public sealed class AzureOpenAITitleGenerationService : ITitleGenerationService
{
  private readonly ChatClient _chatClient;
  private readonly AppSettings _appSettings;
  private readonly ILogger<AzureOpenAITitleGenerationService> _logger;

  private const string TitleGenerationSystemPrompt = """
        You are a title generator for chat conversations. 
        Based on the conversation provided, generate a concise, descriptive title 
        that captures the main topic or intent.
        
        Rules:
        - Maximum 50 characters
        - Use title case
        - Be specific and descriptive
        - Don't use quotation marks
        - Don't include words like "Chat about" or "Discussion on"
        - Respond with ONLY the title, nothing else
        """;

  public AzureOpenAITitleGenerationService(
      AppSettings appSettings,
      ILogger<AzureOpenAITitleGenerationService> logger)
  {
    _appSettings = appSettings;
    _logger = logger;
    _chatClient = CreateChatClient();
  }

  private ChatClient CreateChatClient()
  {
    AzureOpenAIClient azureClient;

    if (_appSettings.AzureAI.UseManagedIdentity)
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_appSettings.AzureAI.Endpoint),
          new DefaultAzureCredential());
    }
    else if (!string.IsNullOrEmpty(_appSettings.AzureAI.ApiKey))
    {
      azureClient = new AzureOpenAIClient(
          new Uri(_appSettings.AzureAI.Endpoint),
          new AzureKeyCredential(_appSettings.AzureAI.ApiKey));
    }
    else
    {
      throw new InvalidOperationException(
          "Azure AI configuration is invalid.");
    }

    // Use a fast, cost-effective model for title generation
    var deploymentName = _appSettings.TitleGeneration?.DeploymentName
                         ?? _appSettings.AzureAI.DeploymentName;
    return azureClient.GetChatClient(deploymentName);
  }

  public async Task<Result<string>> GenerateTitleAsync(
      IReadOnlyList<Core.ConversationAggregate.ChatMessage> messages,
      CancellationToken cancellationToken = default)
  {
    try
    {
      // Build context from first few messages (user + assistant)
      var relevantMessages = messages
          .Where(m => m.Role != ChatRole.System)
          .Take(4) // First 2 exchanges max
          .ToList();

      if (relevantMessages.Count == 0)
      {
        return Result.Error("No messages to generate title from");
      }

      var conversationSummary = string.Join("\n",
          relevantMessages.Select(m => $"{m.Role.Name}: {TruncateContent(m.Content, 200)}"));

      var chatMessages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(TitleGenerationSystemPrompt),
                new UserChatMessage($"Generate a title for this conversation:\n\n{conversationSummary}")
            };

      var options = new ChatCompletionOptions
      {
        MaxOutputTokenCount = 30, // Titles are short
        Temperature = 0.3f        // More deterministic for titles
      };

      var response = await _chatClient.CompleteChatAsync(
          chatMessages,
          options,
          cancellationToken);

      var title = response.Value.Content[0].Text.Trim();

      // Clean up the title
      title = CleanTitle(title);

      _logger.LogInformation(
          "Generated title: '{Title}' (Tokens: {Tokens})",
          title,
          response.Value.Usage.TotalTokenCount);

      return Result.Success(title);
    }
    catch (ClientResultException ex)
    {
      _logger.LogError(ex, "Azure OpenAI API error during title generation");
      return Result.Error($"AI service error: {ex.Message}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error during title generation");
      return Result.Error("Failed to generate title");
    }
  }

  private static string TruncateContent(string content, int maxLength)
  {
    if (content.Length <= maxLength)
      return content;
    return content[..maxLength] + "...";
  }

  private static string CleanTitle(string title)
  {
    // Remove surrounding quotes if present
    title = title.Trim('"', '\'', '"', '"');

    // Ensure max length
    if (title.Length > 50)
      title = title[..47] + "...";

    return title;
  }
}
