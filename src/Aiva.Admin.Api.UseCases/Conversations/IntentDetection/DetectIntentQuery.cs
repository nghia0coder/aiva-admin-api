using Ardalis.Result;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.Interfaces;

namespace Aiva.Admin.Api.UseCases.Conversations.IntentDetection;

public sealed record DetectIntentQuery(
    string Query,
    IReadOnlyList<ChatMessage> ConversationHistory) : IQuery<Result<IntentDetectionResult>>;

public sealed class DetectIntentHandler : IQueryHandler<DetectIntentQuery, Result<IntentDetectionResult>>
{
  private readonly IIntentDetectionService _intentService;

  public DetectIntentHandler(IIntentDetectionService intentService)
  {
    _intentService = intentService;
  }

  public async ValueTask<Result<IntentDetectionResult>> Handle(
      DetectIntentQuery request,
      CancellationToken cancellationToken)
  {
    return await _intentService.DetectIntentAsync(
        request.Query,
        request.ConversationHistory,
        cancellationToken);
  }
}
