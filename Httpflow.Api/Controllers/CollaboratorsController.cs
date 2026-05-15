using System.Security.Claims;
using Httpflow.Api.Dtos.Collaborators;
using Httpflow.Api.Infrastructure.Exceptions;
using Httpflow.Api.Services.Collaborators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Httpflow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/projects/{projectId:int}/collaborators")]
public sealed class CollaboratorsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProjectCollaboratorDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<ProjectCollaboratorDto>> Get(
        int projectId,
        [FromServices] CollaboratorsService collaboratorsService)
    {
        return Ok(collaboratorsService.GetProjectCollaborators(projectId, GetCurrentUserId()));
    }

    [HttpPost]
    [ProducesResponseType<ProjectCollaboratorDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<ProjectCollaboratorDto> Create(
        int projectId,
        [FromBody] AddProjectCollaboratorDto dto,
        [FromServices] CollaboratorsService collaboratorsService)
    {
        var collaborator = collaboratorsService.AddProjectCollaborator(projectId, GetCurrentUserId(), dto);
        return Created($"/api/v1/projects/{projectId}/collaborators/{collaborator.UserId}", collaborator);
    }

    [HttpPut("{userId:int}/role")]
    [ProducesResponseType<ProjectCollaboratorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<ProjectCollaboratorDto> UpdateRole(
        int projectId,
        int userId,
        [FromBody] UpdateProjectCollaboratorRoleDto dto,
        [FromServices] CollaboratorsService collaboratorsService)
    {
        return Ok(collaboratorsService.UpdateProjectCollaboratorRole(
            projectId,
            userId,
            GetCurrentUserId(),
            dto));
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
}
