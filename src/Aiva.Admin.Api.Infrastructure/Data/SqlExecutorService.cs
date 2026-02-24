using System.Data;
using Aiva.Admin.Api.Core.Interfaces;
using Ardalis.Result;
using Microsoft.Extensions.Logging;

namespace Aiva.Admin.Api.Infrastructure.Data;

public sealed class SqlExecutorService : ISqlExecutorService
{
  private readonly ISqlExecutorDbConnectionFactory _connectionFactory;
  private readonly ILogger<SqlExecutorService> _logger;

  public SqlExecutorService(
      ISqlExecutorDbConnectionFactory connectionFactory,
      ILogger<SqlExecutorService> logger)
  {
    _connectionFactory = connectionFactory;
    _logger = logger;
  }

  public async Task<Result<List<Dictionary<string, object>>>> ExecuteQueryAsync(
      string sql,
      CancellationToken cancellationToken = default)
  {
    try
    {
      _logger.LogInformation("Executing dynamic SQL: {Sql}", sql);

      var results = new List<Dictionary<string, object>>();

      using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
      using var command = connection.CreateCommand();
      command.CommandText = sql;
      command.CommandType = CommandType.Text;

      using var reader = command.ExecuteReader();

      while (reader.Read())
      {
        var row = new Dictionary<string, object>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
          var columnName = reader.GetName(i);
          var value = reader.GetValue(i);
          row[columnName] = value is DBNull ? null! : value;
        }
        results.Add(row);
      }

      _logger.LogInformation("SQL execution completed. Returned {Count} rows.", results.Count);
      return Result.Success(results);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error executing dynamic SQL: {Sql}", sql);
      return Result.Error($"Database error: {ex.Message}");
    }
  }
}
