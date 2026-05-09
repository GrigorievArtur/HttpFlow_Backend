using System.ComponentModel.DataAnnotations;

namespace Httpflow.Api.Dtos.Users;

public record LoginUserDto(
    [param: Required, EmailAddress, StringLength(320)] string Email,
    [param: Required, MinLength(8), StringLength(255)] string Password);
