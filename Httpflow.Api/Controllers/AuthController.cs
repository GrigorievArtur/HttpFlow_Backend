using Httpflow.Api.Dtos.Users;
using Httpflow.Api.Services.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Httpflow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<AuthResponseDto> Register(
        [FromBody] RegisterUserDto dto,
        [FromServices] AuthService authService)
    {
        var authResponse = authService.Register(dto);
        return Created($"/api/v1/users/{authResponse.User.Id}", authResponse);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<AuthResponseDto> Login(
        [FromBody] LoginUserDto dto,
        [FromServices] AuthService authService)
    {
        return Ok(authService.Login(dto));
    }
}
