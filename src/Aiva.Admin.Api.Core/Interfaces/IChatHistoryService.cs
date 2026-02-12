using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.Core.Interfaces;

public interface IChatHistoryService
{
  Task<string> SerializeChatHistoryAsync(List<ChatMessage> messages);
}
