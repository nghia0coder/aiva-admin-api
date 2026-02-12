using System.Data;
using Ardalis.Result;

namespace Aiva.Admin.Api.Core.Interfaces;

public interface ISqlExecutorService
{
  /// <summary>
  /// Executes a SELECT query and returns the results as a list of dictionaries.
  /// Each dictionary represents a row where the key is the column name.
  /// </summary>
  Task<Result<List<Dictionary<string, object>>>> ExecuteQueryAsync(
      string sql,
      CancellationToken cancellationToken = default);
}
