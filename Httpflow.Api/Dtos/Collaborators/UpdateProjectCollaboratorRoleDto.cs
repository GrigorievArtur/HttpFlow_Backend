using System.ComponentModel.DataAnnotations;

namespace Httpflow.Api.Dtos.Collaborators;

public record UpdateProjectCollaboratorRoleDto(
    [param: Required, StringLength(32)] string Role);
