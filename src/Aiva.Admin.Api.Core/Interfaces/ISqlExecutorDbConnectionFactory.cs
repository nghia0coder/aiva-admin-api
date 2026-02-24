using System.Data;

namespace Aiva.Admin.Api.Core.Interfaces;

public interface ISqlExecutorDbConnectionFactory
{
  Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
