using System.Security.Claims;
using Aiva.Admin.Api.Core.UserAggregate;
using Aiva.Admin.Api.Core.UserAggregate.Specifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Aiva.Admin.Api.Infrastructure.Identity;

public class UserClaimsTransformation : IClaimsTransformation
{
  private readonly IRepository<User> _userRepository;
  private readonly IMemoryCache _cache;
  private readonly ILogger<UserClaimsTransformation> _logger;

  // Custom claim type for internal user ID
  public const string InternalUserIdClaimType = "internal_user_id";

  public UserClaimsTransformation(
      IRepository<User> userRepository,
      IMemoryCache cache,
      ILogger<UserClaimsTransformation> logger)
  {
    _userRepository = userRepository;
    _cache = cache;
    _logger = logger;
  }

  public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    // Only process authenticated users
    if (principal.Identity?.IsAuthenticated != true)
    {
      return principal;
    }

    // Get Azure AD Object ID from claims
    var azureAdOid = principal.FindFirstValue("oid")
        ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");

    if (string.IsNullOrEmpty(azureAdOid))
    {
      _logger.LogWarning("Authenticated user has no 'oid' claim");
      return principal;
    }

    // Check if we already have the internal user ID (prevent duplicate processing)
    if (principal.HasClaim(c => c.Type == InternalUserIdClaimType))
    {
      return principal;
    }

    // Try to get from cache first
    var cacheKey = $"user_claims_{azureAdOid}";
    if (!_cache.TryGetValue(cacheKey, out int internalUserId))
    {
      // Cache miss - sync with database
      var user = await EnsureUserExistsAsync(principal, azureAdOid);
      internalUserId = user.Id.Value;

      // Cache for 15 minutes
      _cache.Set(cacheKey, internalUserId, TimeSpan.FromMinutes(15));
    }

    // Clone the identity and add the internal user ID claim
    var claimsIdentity = principal.Identity as ClaimsIdentity;
    if (claimsIdentity != null)
    {
      claimsIdentity.AddClaim(new Claim(InternalUserIdClaimType, internalUserId.ToString()));
    }

    return principal;
  }

  private async Task<User> EnsureUserExistsAsync(ClaimsPrincipal principal, string azureAdOid)
  {
    var azureAdObjectId = AzureAdObjectId.From(azureAdOid);
    var spec = new UserByAzureAdObjectIdSpec(azureAdObjectId);

    var existingUser = await _userRepository.SingleOrDefaultAsync(spec);

    if (existingUser is not null)
    {
      // Update last login timestamp
      existingUser.RecordLogin();

      // Optionally sync profile changes from Azure AD
      var email = principal.FindFirstValue("preferred_username")
          ?? principal.FindFirstValue(ClaimTypes.Email)
          ?? "";
      var displayName = principal.FindFirstValue("name")
          ?? principal.FindFirstValue(ClaimTypes.Name)
          ?? "Unknown";
      var firstName = principal.FindFirstValue(ClaimTypes.GivenName);
      var lastName = principal.FindFirstValue(ClaimTypes.Surname);

      existingUser.UpdateFromAzureAd(email, displayName, firstName, lastName);

      await _userRepository.UpdateAsync(existingUser);

      _logger.LogDebug("User {UserId} logged in", existingUser.Id.Value);
      return existingUser;
    }

    // Create new user (first-time login)
    var newEmail = principal.FindFirstValue("preferred_username")
        ?? principal.FindFirstValue(ClaimTypes.Email)
        ?? "";
    var newDisplayName = principal.FindFirstValue("name")
        ?? principal.FindFirstValue(ClaimTypes.Name)
        ?? "Unknown";
    var newFirstName = principal.FindFirstValue(ClaimTypes.GivenName);
    var newLastName = principal.FindFirstValue(ClaimTypes.Surname);

    var newUser = User.Create(
        azureAdObjectId,
        newEmail,
        newDisplayName,
        newFirstName,
        newLastName);
    newUser.RecordLogin();

    await _userRepository.AddAsync(newUser);

    _logger.LogInformation(
        "Created new user {UserId} for Azure AD user {AzureAdOid}",
        newUser.Id.Value,
        azureAdOid);

    return newUser;
  }
}
