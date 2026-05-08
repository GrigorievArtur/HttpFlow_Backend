namespace Httpflow.Api.Dtos.Users;

public record AuthResponseDto(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserDto User);
