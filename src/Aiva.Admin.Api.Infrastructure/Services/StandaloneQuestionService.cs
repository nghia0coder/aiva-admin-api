using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class StandaloneQuestionService(
    IJsonParseService jsonParseService,
    ILogger<StandaloneQuestionService> logger) : IStandaloneQuestionService
{
    public StandaloneQuestionDto ParseStandaloneQuestion(string jsonContent)
    {
        var dataStandalone = jsonParseService.Parse<StandaloneQuestionDto>(jsonContent);

        logger.LogDebug("Parsed standalone question. QueryString: {QueryString}, StandaloneQuestion: {StandaloneQuestion}",
            dataStandalone.QueryString, dataStandalone.StandaloneQuestion);

        dataStandalone.KeyWords = ExtractKeywords(dataStandalone.QueryString);

        return dataStandalone;
    }

    private static List<string> ExtractKeywords(string? queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return new List<string>();
        }

        return queryString
            .Split(';')
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(keyword => keyword.Trim())
            .ToList();
    }
}
