namespace Aiva.Admin.Api.Core.Interfaces;

using Commons.Models;
using Commons.Results;

public interface IRetrievalService
{
  /// <summary>
  /// Retrieves relevant document chunks using intelligent search strategy
  /// </summary>
  Task<Result<RetrievalResult>> RetrieveContextAsync(
      string query,
      RetrievalOptions? options = null,
      CancellationToken cancellationToken = default);
}
