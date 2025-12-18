using Aiva.Admin.Api.Core.UserAggregate;
using Aiva.Admin.Api.Core.UserAggregate.Specifications;

namespace Aiva.Admin.Api.UseCases.Users.SyncCurrentUser;

public class SyncCurrentUserHandler : ICommandHandler<SyncCurrentUserCommand, Result<UserDto>>
{
  private readonly IRepository<User> _repository;

  public SyncCurrentUserHandler(IRepository<User> repository)
  {
    _repository = repository;
  }

  public async ValueTask<Result<UserDto>> Handle(
      SyncCurrentUserCommand request,
      CancellationToken ct)
  {
    var azureAdObjectId = AzureAdObjectId.From(request.AzureAdObjectId);
    var spec = new UserByAzureAdObjectIdSpec(azureAdObjectId);

    var existingUser = await _repository.SingleOrDefaultAsync(spec, ct);

    if (existingUser is not null)
    {
      // Update user info from Azure AD (in case profile changed)
      existingUser.UpdateFromAzureAd(
          request.Email,
          request.DisplayName,
          request.FirstName,
          request.LastName);
      existingUser.RecordLogin();

      await _repository.UpdateAsync(existingUser, ct);

      return new UserDto(
          existingUser.Id.Value,
          existingUser.Email,
          existingUser.DisplayName,
          existingUser.FirstName,
          existingUser.LastName,
          existingUser.Status.Name,
          existingUser.LastLoginAt,
          existingUser.CreatedOnUtc);
    }

    // Create new user (first-time login)
    var newUser = User.Create(
        azureAdObjectId,
        request.Email,
        request.DisplayName,
        request.FirstName,
        request.LastName);
    newUser.RecordLogin();

    await _repository.AddAsync(newUser, ct);

    return new UserDto(
        newUser.Id.Value,
        newUser.Email,
        newUser.DisplayName,
        newUser.FirstName,
        newUser.LastName,
        newUser.Status.Name,
        newUser.LastLoginAt,
        newUser.CreatedOnUtc);
  }
}
