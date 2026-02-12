using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

namespace Aiva.Admin.Api.Core.Interfaces;

public interface IPromptTemplateService
{
    string ReplacePromptByKey(string prompt, ReplacePromptDto replacePromptDto);
    string BuildEnhancedPrompt(List<Dictionary<string, object>> sqlResult, ReplacePromptDto dto);
}
