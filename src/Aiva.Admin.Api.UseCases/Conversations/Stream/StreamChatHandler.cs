using System.Text.Json;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.ConversationAggregate.Specifications;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public class StreamChatHandler(
IRepository<Conversation> repository,
IChatCompletionService chatService,
ISystemPromptService systemPromptService,
ISqlExecutorService sqlExecutorService,
IPromptTemplateService promptTemplateService,
IDataFormatterService dataFormatterService,
IChatHistoryService chatHistoryService,
IChartGenerationService chartGenerationService,
ILogger<StreamChatHandler> logger)
: IRequestHandler<StreamChatCommand, Result<StreamChatResponse>>
{
  public async ValueTask<Result<StreamChatResponse>> Handle(StreamChatCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var spec = new ConversationByIdWithMessagesSpec(ConversationId.From(request.ConversationId));
      var conversation = await repository.FirstOrDefaultAsync(spec, cancellationToken);

      if (conversation is null)
      {
        return Result.NotFound("Conversation not found");
      }

      var recentMessages = conversation.GetRecentMessages();
      var chatHistory = await chatHistoryService.SerializeChatHistoryAsync(recentMessages.ToList());

      conversation.AddMessage(ChatRole.User, request.Message);

      var standaloneMessage = promptTemplateService.ReplacePromptByKey(
          PromptTemplates.MessageTemplateForGenerateQuestion,
          new ReplacePromptDto
          {
            ChatInput = request.Message,
            ChatHistory = chatHistory
          });

      var standaloneMessageResult = await chatService.GetCompletionAsync("", standaloneMessage);

      var systemPrompt = await systemPromptService.GetActivePromptContentAsync(
          SystemPromptKey.From("data-assistant"),
          cancellationToken);

      var responseAnswer = await chatService.GetCompletionAsync(systemPrompt, standaloneMessageResult);

      var sqlToExecute = dataFormatterService.ExtractSql(responseAnswer) ?? responseAnswer;

      var sqlQueryResult = await sqlExecutorService.ExecuteQueryAsync(sqlToExecute);

      var response = new StreamChatResponse();
      string finalAssistantMessage = responseAnswer;
      ChartMessageData? chartMessageData = null;

      if (sqlQueryResult.IsSuccess && sqlQueryResult.Value.Any())
      {
        var markdownTable = dataFormatterService.FormatAsMarkdownTable(sqlQueryResult.Value);
        response.MarkdownTable = markdownTable;

        var enhancedPromptDto = new ReplacePromptDto
        {
          ChatInput = request.Message,
          ChatHistory = chatHistory,
          ResultData = markdownTable,
          SqlQuery = sqlToExecute,
          StandaloneQuestion = standaloneMessageResult
        };

        var messageCognitiveOutput = promptTemplateService.BuildEnhancedPrompt(sqlQueryResult.Value, enhancedPromptDto);

        var responseCognitive = await chatService.GetCompletionAsync(
            PromptTemplates.SystemTemplateCognitiveOutput,
            messageCognitiveOutput);

        var cognitiveOutputDto = JsonSerializer.Deserialize<CognitiveOutputDto>(responseCognitive,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        response.TextResponse = cognitiveOutputDto?.Answer ?? responseAnswer;

        // Generate chart if needed
        if (cognitiveOutputDto != null && !string.IsNullOrEmpty(cognitiveOutputDto.ChartType))
        {
          var chartConfig = chartGenerationService.GenerateChartConfig(
              sqlQueryResult.Value,
              cognitiveOutputDto.ChartXAxis ?? "",
              cognitiveOutputDto.ChartYAxis ?? "",
              cognitiveOutputDto.ChartType);

          response.HasChart = true;
          response.ChartConfig = chartConfig;
          response.ChartType = cognitiveOutputDto.ChartType;

          // Prepare chart data for storage
          chartMessageData = new ChartMessageData
          {
            TextResponse = response.TextResponse,
            MarkdownTable = markdownTable,
            HasChart = true,
            ChartConfig = chartConfig,
            ChartType = cognitiveOutputDto.ChartType
          };
        }

        // Create comprehensive response message for conversation history
        var responseMessageParts = new List<string>();

        if (!string.IsNullOrEmpty(response.TextResponse))
        {
          responseMessageParts.Add(response.TextResponse);
        }

        if (!string.IsNullOrEmpty(markdownTable))
        {
          responseMessageParts.Add("\n\n" + markdownTable);
        }

        finalAssistantMessage = string.Join("", responseMessageParts);
      }
      else
      {
        response.TextResponse = responseAnswer;
      }

      // Save comprehensive assistant message with chart data if available
      var assistantMessage = conversation.AddMessage(ChatRole.Assistant, finalAssistantMessage);

      if (chartMessageData != null)
      {
        assistantMessage.SetChartResponse(chartMessageData);
      }

      if (conversation.IsReadyForTitleGeneration())
      {
        conversation.QueueForTitleGeneration();
      }

      await repository.UpdateAsync(conversation, cancellationToken);

      return Result.Success(response);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error processing stream chat for conversation {ConversationId}", request.ConversationId);
      return Result.Error(ex.Message);
    }
  }
}
