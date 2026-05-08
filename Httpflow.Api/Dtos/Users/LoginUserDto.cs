using System.ComponentModel.DataAnnotations;

namespace Httpflow.Api.Dtos.Users;

public record LoginUserDto(
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: Required, MinLength(8), StringLength(255)] string Password);
