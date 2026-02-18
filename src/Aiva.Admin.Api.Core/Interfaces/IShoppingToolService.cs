using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

namespace Aiva.Admin.Api.Core.Interfaces;

public interface IShoppingToolService
{
  IEnumerable<ToolDefinition> GetAvailableTools();
  Task<Result<string>> ExecuteToolAsync(string functionName, Dictionary<string, object> parameters, string userId, CancellationToken cancellationToken = default);
}
