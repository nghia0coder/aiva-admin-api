using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Common;

using Core.Interfaces;
using Core.UserAggregate;

/// <summary>
/// Base endpoint for authenticated requests that require UserId.
/// Provides CurrentUserId property via property injection.
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract class AuthenticatedEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
  /// <summary>
  /// Injected by FastEndpoints via property injection
  /// </summary>
  public ICurrentUserService CurrentUserService { get; set; } = null!;

  /// <summary> 
  /// Gets the current authenticated user's ID. Returns null if not authenticated.
  /// </summary>
  protected UserId? CurrentUserId => CurrentUserService.UserId;

  /// <summary>
  /// Get the current user full name. Returns null if not authenticated.
  /// </summary>
  public string? FullName => CurrentUserService.DisplayName;

  /// <summary>
  /// Gets the current authenticated user's ID. Throws if not authenticated.
  /// Use this when you've already verified authentication.
  /// </summary>
  protected UserId RequiredUserId => CurrentUserService.UserId
      ?? throw new UnauthorizedAccessException("User not authenticated or UserId not found");

  /// <summary>
  /// Checks if current user is authenticated with valid UserId
  /// </summary>
  protected bool IsAuthenticated => CurrentUserService.IsAuthenticated
      && CurrentUserService.UserId.HasValue;

  /// <summary>
  /// Returns a standardized Unauthorized problem result
  /// </summary>
  protected ProblemHttpResult UnauthorizedResult(string detail = "User not authenticated")
      => TypedResults.Problem(
          detail: detail,
          statusCode: StatusCodes.Status401Unauthorized);
}

/// <summary>
/// Base endpoint for authenticated requests without request body.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
public abstract class AuthenticatedEndpointWithoutRequest<TResponse> : EndpointWithoutRequest<TResponse>
{
  /// <summary>
  /// Injected by FastEndpoints via property injection
  /// </summary>
  public ICurrentUserService CurrentUserService { get; set; } = null!;

  /// <summary>
  /// Gets the current authenticated user's ID. Returns null if not authenticated.
  /// </summary>
  protected UserId? CurrentUserId => CurrentUserService.UserId;

  /// <summary>
  /// Gets the current authenticated user's ID. Throws if not authenticated.
  /// </summary>
  protected UserId RequiredUserId => CurrentUserService.UserId
      ?? throw new UnauthorizedAccessException("User not authenticated or UserId not found");

  /// <summary>
  /// Checks if current user is authenticated with valid UserId
  /// </summary>
  protected bool IsAuthenticated => CurrentUserService.IsAuthenticated
      && CurrentUserService.UserId.HasValue;

  /// <summary>
  /// Returns a standardized Unauthorized problem result
  /// </summary>
  protected ProblemHttpResult UnauthorizedResult(string detail = "User not authenticated")
      => TypedResults.Problem(
          detail: detail,
          statusCode: StatusCodes.Status401Unauthorized);
}

/// <summary>
/// Base endpoint for authenticated requests without request body.
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TMapper">The response type</typeparam>
public abstract class AuthenticatedEndpointWithMapper<TRequest, TResponse, TMapper> :
  Endpoint<TRequest, TResponse, TMapper>
  where TRequest : notnull
  where TResponse : notnull
  where TMapper : class, IMapper, new()
{
  /// <summary>
  /// Injected by FastEndpoints via property injection
  /// </summary>
  public ICurrentUserService CurrentUserService { get; set; } = null!;

  /// <summary>
  /// Gets the current authenticated user's ID. Returns null if not authenticated.
  /// </summary>
  protected UserId? CurrentUserId => CurrentUserService.UserId;

  /// <summary>
  /// Gets the current authenticated user's ID. Throws if not authenticated.
  /// </summary>
  protected UserId RequiredUserId => CurrentUserService.UserId
      ?? throw new UnauthorizedAccessException("User not authenticated or UserId not found");

  /// <summary>
  /// Checks if current user is authenticated with valid UserId
  /// </summary>
  protected bool IsAuthenticated => CurrentUserService.IsAuthenticated
      && CurrentUserService.UserId.HasValue;

  /// <summary>
  /// Returns a standardized Unauthorized problem result
  /// </summary>
  protected ProblemHttpResult UnauthorizedResult(string detail = "User not authenticated")
      => TypedResults.Problem(
          detail: detail,
          statusCode: StatusCodes.Status401Unauthorized);
}
