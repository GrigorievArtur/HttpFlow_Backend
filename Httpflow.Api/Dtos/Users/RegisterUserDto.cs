using System.ComponentModel.DataAnnotations;

namespace Httpflow.Api.Dtos.Users;

public record RegisterUserDto(
    [property: Required, StringLength(255)] string Firstname,
    [property: Required, StringLength(255)] string Lastname,
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: Required, MinLength(8), StringLength(255)] string Password);
