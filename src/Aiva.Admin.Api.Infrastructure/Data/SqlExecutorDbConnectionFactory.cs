using System.Data;
using System.Data.Common;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;

namespace Aiva.Admin.Api.Infrastructure.Data;

public sealed class SqlExecutorDbConnectionFactory : ISqlExecutorDbConnectionFactory
{
  private readonly string _connectionString;

  public SqlExecutorDbConnectionFactory(IOptions<AppSettings> appSettings)
  {
    var sqlExecutorConnection = appSettings.Value.ConnectionStrings.SqlExecutorConnection;

    if (string.IsNullOrEmpty(sqlExecutorConnection))
    {
      throw new InvalidOperationException("SqlExecutorConnection is not configured.");
    }

    _connectionString = sqlExecutorConnection;
  }

  public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
  {
    DbConnection connection = new SqlConnection(_connectionString);

    await connection.OpenAsync(cancellationToken);
    return connection;
  }
}
