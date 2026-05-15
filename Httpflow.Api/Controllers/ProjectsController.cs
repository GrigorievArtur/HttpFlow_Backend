using System.Security.Claims;
using Httpflow.Api.Dtos.Projects;
using Httpflow.Api.Infrastructure.Exceptions;
using Httpflow.Api.Services.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Httpflow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/projects")]
public sealed class ProjectsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<ProjectDto> Create(
        [FromBody] CreateProjectDto dto,
        [FromServices] ProjectsService projectsService)
    {
        var project = projectsService.CreateProject(GetCurrentUserId(), dto);
        return Created($"/api/v1/projects/{project.Id}", project);
    }

    [HttpGet]
    [ProducesResponseType<List<ProjectDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public List<ProjectDto> Get(
        [FromServices] ProjectsService projectsService,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 5)
    {
        var projects = projectsService.GetPagedProjects(GetCurrentUserId(), pageNumber, pageSize);
        return projects;
    }

    [HttpGet("{projectId:int}")]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<ProjectDto> GetById(
        int projectId,
        [FromServices] ProjectsService projectsService)
    {
        return Ok(projectsService.GetProjectById(projectId, GetCurrentUserId()));
    }

    [HttpPut("{projectId:int}")]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public ActionResult<ProjectDto> Update(
        int projectId,
        [FromBody] UpdateProjectDto dto,
        [FromServices] ProjectsService projectsService)
    {
        return Ok(projectsService.UpdateProject(projectId, GetCurrentUserId(), dto));
    }

    [HttpDelete("{projectId:int}")]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<ProjectDto> Delete(
        int projectId,
        [FromServices] ProjectsService projectsService)
    {
        return Ok(projectsService.DeleteProject(projectId, GetCurrentUserId()));
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
