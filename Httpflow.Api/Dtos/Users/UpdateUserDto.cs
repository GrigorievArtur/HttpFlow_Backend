using System.ComponentModel.DataAnnotations;

namespace Httpflow.Api.Dtos.Users;

public record UpdateUserDto(
    [param: Required, StringLength(255)] string Firstname,
    [param: Required, StringLength(255)] string Lastname,
    [param: Required, EmailAddress, StringLength(320)] string Email);
