namespace Aiva.Admin.Api.UseCases.Users.SyncCurrentUser;

public record SyncCurrentUserCommand(
    string AzureAdObjectId,
    string Email,
    string DisplayName,
    string? FirstName,
    string? LastName) : ICommand<Result<UserDto>>;
