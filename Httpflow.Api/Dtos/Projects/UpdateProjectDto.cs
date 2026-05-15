using System.ComponentModel.DataAnnotations;

namespace Httpflow.Api.Dtos.Projects;

public record UpdateProjectDto(
    [param: Required, StringLength(255)] string Name,
    [param: Required] string Value);
