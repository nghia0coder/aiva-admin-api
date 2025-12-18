namespace Aiva.Admin.Api.UseCases.Users;

public record UserDto(
    int Id,
    string Email,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string Status,
    DateTime? LastLoginAt,
    DateTime CreatedOnUtc);
