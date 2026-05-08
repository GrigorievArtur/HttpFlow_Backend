using System.ComponentModel.DataAnnotations;

namespace Httpflow.Api.Dtos.Users;

public record UserDto(
    [property: Range(1, int.MaxValue)] int Id,
    [property: Required, StringLength(255)] string Firstname,
    [property: Required, StringLength(255)] string Lastname,
    [property: Required, EmailAddress, StringLength(320)] string Email);
