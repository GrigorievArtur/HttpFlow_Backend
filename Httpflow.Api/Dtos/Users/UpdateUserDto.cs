using System.ComponentModel.DataAnnotations;

namespace Httpflow.Api.Dtos.Users;

public record UpdateUserDto(
    [property: Required, StringLength(255)] string Firstname,
    [property: Required, StringLength(255)] string Lastname,
    [property: Required, EmailAddress, StringLength(320)] string Email);
