using System.Text;
using Aiva.Admin.Api.Core.ConversationAggregate.Constants;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class PromptTemplateService : IPromptTemplateService
{
    public string ReplacePromptByKey(string prompt, ReplacePromptDto replacePromptDto)
    {
        if (string.IsNullOrEmpty(prompt)) return string.Empty;
        
        var result = new StringBuilder(prompt);

        result.Replace(MessageReplaceKeys.ChatInput, replacePromptDto.ChatInput ?? string.Empty);
        result.Replace(MessageReplaceKeys.ChatHistory, replacePromptDto.ChatHistory ?? string.Empty);
        result.Replace(MessageReplaceKeys.SqlQuery, replacePromptDto.SqlQuery ?? string.Empty);
        result.Replace(MessageReplaceKeys.ResultData, replacePromptDto.ResultData ?? string.Empty);
        result.Replace(MessageReplaceKeys.StandaloneQuestion, replacePromptDto.StandaloneQuestion ?? string.Empty);
        result.Replace(MessageReplaceKeys.SystemTime, DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss"));
        result.Replace(MessageReplaceKeys.RowCount, replacePromptDto.RowCount.ToString());
        result.Replace(MessageReplaceKeys.ColumnNames, replacePromptDto.ColumnNames ?? string.Empty);
        
        return result.ToString();
    }

    public string BuildEnhancedPrompt(List<Dictionary<string, object>> sqlResult, ReplacePromptDto dto)
    {
        dto.RowCount = sqlResult.Count;
        dto.ColumnNames = sqlResult.Any() ? string.Join(", ", sqlResult[0].Keys) : "";

        var result = new StringBuilder(PromptTemplates.MessageTemplateCognitiveOutput);

        // Add new replacements
        result.Replace("@{row_count}", dto.RowCount.ToString());
        result.Replace("@{column_names}", dto.ColumnNames ?? "");

        return ReplacePromptByKey(result.ToString(), dto);
    }
}
