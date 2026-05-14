using System.Security.Claims;
using Httpflow.Api.Dtos.Users;
using Httpflow.Api.Infrastructure.Exceptions;
using Httpflow.Api.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Httpflow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserDto>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<UserDto>> GetAll([FromServices] UserService userService)
    {
        return Ok(userService.GetUsers());
    }

    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserDto> GetMe([FromServices] UserService userService)
    {
        return Ok(userService.GetUserById(GetCurrentUserId()));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<UserDto> GetById(int id, [FromServices] UserService userService)
    {
        EnsureCurrentUserOwnsResource(id);
        return Ok(userService.GetUserById(id));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<UserDto> Update(
        int id,
        [FromBody] UpdateUserDto dto,
        [FromServices] UserService userService)
    {
        EnsureCurrentUserOwnsResource(id);
        return Ok(userService.UpdateUser(id, dto));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Delete(int id, [FromServices] UserService userService)
    {
        EnsureCurrentUserOwnsResource(id);
        userService.DeleteUser(id);
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("sub");

        if (!int.TryParse(claimValue, out var userId))
        {
            throw new UnauthorizedApiException("The current access token is invalid.");
        }

        return userId;
    }

    private void EnsureCurrentUserOwnsResource(int resourceUserId)
    {
        if (GetCurrentUserId() != resourceUserId)
        {
            throw new ForbiddenApiException("You can only access your own user resource.");
        }
    }
}
