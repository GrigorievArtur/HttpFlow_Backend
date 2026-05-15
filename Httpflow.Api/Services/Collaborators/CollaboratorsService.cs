using Httpflow.Api.Dtos.Collaborators;
using Httpflow.Api.Infrastructure.Exceptions;
using MySqlConnector;

namespace Httpflow.Api.Services.Collaborators;

public sealed class CollaboratorsService
{
    private const string AdminRole = "Admin";
    private const string MemberRole = "Member";
    private const string VisitorRole = "Visitor";

    private readonly string _connectionString;

    public CollaboratorsService()
    {
        _connectionString = Environment.GetEnvironmentVariable("SQL_DATABASE_URL")
                            ?? throw new InvalidOperationException("Missing required environment variable: SQL_DATABASE_URL");
    }

    public IReadOnlyList<ProjectCollaboratorDto> GetProjectCollaborators(int projectId, int currentUserId)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        EnsureProjectTeammatesRoleColumn(connection);
        EnsureProjectAccess(projectId, currentUserId, connection);

        return GetProjectCollaborators(projectId, connection);
    }

    public ProjectCollaboratorDto AddProjectCollaborator(
        int projectId,
        int currentUserId,
        AddProjectCollaboratorDto dto)
    {
        var role = NormalizeRole(dto.Role);

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        EnsureProjectTeammatesRoleColumn(connection);
        EnsureCanManageProjectCollaborators(projectId, currentUserId, connection);

        var ownerUserId = GetProjectOwnerUserId(projectId, connection);
        var collaboratorUserId = GetUserIdByEmail(dto.Email.Trim(), connection);
        if (collaboratorUserId == ownerUserId)
        {
            throw new ConflictException("The project owner already has admin access.");
        }

        const string insertScript = """
            INSERT INTO `ProjectTeammates` (`ProjectId`, `UserId`, `Role`)
            VALUES (@projectId, @userId, @role);
            """;

        using var command = new MySqlCommand(insertScript, connection);
        command.Parameters.AddWithValue("@projectId", projectId);
        command.Parameters.AddWithValue("@userId", collaboratorUserId);
        command.Parameters.AddWithValue("@role", role);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ConflictException("That user is already a collaborator on this project.");
        }

        return GetProjectCollaborator(projectId, collaboratorUserId, connection);
    }

    public ProjectCollaboratorDto UpdateProjectCollaboratorRole(
        int projectId,
        int collaboratorUserId,
        int currentUserId,
        UpdateProjectCollaboratorRoleDto dto)
    {
        var role = NormalizeRole(dto.Role);

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        EnsureProjectTeammatesRoleColumn(connection);
        EnsureCanManageProjectCollaborators(projectId, currentUserId, connection);

        if (GetProjectOwnerUserId(projectId, connection) == collaboratorUserId)
        {
            throw new ForbiddenApiException("The project owner's admin role cannot be changed.");
        }

        const string updateScript = """
            UPDATE `ProjectTeammates`
            SET `Role` = @role
            WHERE `ProjectId` = @projectId
                AND `UserId` = @userId;
            """;

        using var command = new MySqlCommand(updateScript, connection);
        command.Parameters.AddWithValue("@projectId", projectId);
        command.Parameters.AddWithValue("@userId", collaboratorUserId);
        command.Parameters.AddWithValue("@role", role);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new ResourceNotFoundException("Collaborator was not found on this project.");
        }

        return GetProjectCollaborator(projectId, collaboratorUserId, connection);
    }

    private static IReadOnlyList<ProjectCollaboratorDto> GetProjectCollaborators(
        int projectId,
        MySqlConnection connection)
    {
        const string selectScript = """
            SELECT *
            FROM (
                SELECT u.`Id` AS `UserId`,
                       u.`Firstname`,
                       u.`Lastname`,
                       u.`Email`,
                       'Admin' AS `Role`,
                       1 AS `IsOwner`
                FROM `Projects` p
                INNER JOIN `Users` u ON u.`Id` = p.`OwnerUserId`
                WHERE p.`Id` = @projectId

                UNION ALL

                SELECT u.`Id` AS `UserId`,
                       u.`Firstname`,
                       u.`Lastname`,
                       u.`Email`,
                       pt.`Role`,
                       0 AS `IsOwner`
                FROM `ProjectTeammates` pt
                INNER JOIN `Users` u ON u.`Id` = pt.`UserId`
                WHERE pt.`ProjectId` = @projectId
            ) AS collaborators
            ORDER BY `IsOwner` DESC, `Firstname`, `Lastname`, `Email`;
            """;

        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@projectId", projectId);

        using var reader = command.ExecuteReader();
        var collaborators = new List<ProjectCollaboratorDto>();
        while (reader.Read())
        {
            collaborators.Add(ReadProjectCollaborator(reader));
        }

        return collaborators;
    }

    private static ProjectCollaboratorDto GetProjectCollaborator(
        int projectId,
        int userId,
        MySqlConnection connection)
    {
        const string selectScript = """
            SELECT u.`Id` AS `UserId`,
                   u.`Firstname`,
                   u.`Lastname`,
                   u.`Email`,
                   pt.`Role`,
                   0 AS `IsOwner`
            FROM `ProjectTeammates` pt
            INNER JOIN `Users` u ON u.`Id` = pt.`UserId`
            WHERE pt.`ProjectId` = @projectId
                AND pt.`UserId` = @userId
            LIMIT 1;
            """;

        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@projectId", projectId);
        command.Parameters.AddWithValue("@userId", userId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new ResourceNotFoundException("Collaborator was not found on this project.");
        }

        return ReadProjectCollaborator(reader);
    }

    private static ProjectCollaboratorDto ReadProjectCollaborator(MySqlDataReader reader)
    {
        return new ProjectCollaboratorDto(
            reader.GetInt32("UserId"),
            reader.GetString("Firstname"),
            reader.GetString("Lastname"),
            reader.GetString("Email"),
            reader.GetString("Role"),
            Convert.ToBoolean(reader["IsOwner"]));
    }

    private static void EnsureProjectAccess(int projectId, int userId, MySqlConnection connection)
    {
        if (GetProjectRole(projectId, userId, connection) is null)
        {
            throw new ForbiddenApiException("You do not have access to this project.");
        }
    }

    private static void EnsureCanManageProjectCollaborators(
        int projectId,
        int userId,
        MySqlConnection connection)
    {
        if (GetProjectRole(projectId, userId, connection) != AdminRole)
        {
            throw new ForbiddenApiException("Only project admins can manage collaborators.");
        }
    }

    private static string? GetProjectRole(int projectId, int userId, MySqlConnection connection)
    {
        var ownerUserId = GetProjectOwnerUserId(projectId, connection);
        if (ownerUserId == userId)
        {
            return AdminRole;
        }

        const string selectScript = """
            SELECT `Role`
            FROM `ProjectTeammates`
            WHERE `ProjectId` = @projectId
                AND `UserId` = @userId
            LIMIT 1;
            """;

        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@projectId", projectId);
        command.Parameters.AddWithValue("@userId", userId);

        var role = command.ExecuteScalar()?.ToString();
        return string.IsNullOrWhiteSpace(role) ? null : NormalizeRole(role);
    }

    private static int GetProjectOwnerUserId(int projectId, MySqlConnection connection)
    {
        const string selectScript = """
            SELECT `OwnerUserId`
            FROM `Projects`
            WHERE `Id` = @projectId
            LIMIT 1;
            """;

        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@projectId", projectId);

        var ownerUserId = command.ExecuteScalar();
        if (ownerUserId is null)
        {
            throw new ResourceNotFoundException($"Project with id {projectId} was not found.");
        }

        return Convert.ToInt32(ownerUserId);
    }

    private static int GetUserIdByEmail(string email, MySqlConnection connection)
    {
        const string selectScript = """
            SELECT `Id`
            FROM `Users`
            WHERE `Email` = @email
            LIMIT 1;
            """;

        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@email", email);

        var userId = command.ExecuteScalar();
        if (userId is null)
        {
            throw new ResourceNotFoundException($"User with email {email} was not found.");
        }

        return Convert.ToInt32(userId);
    }

    private static string NormalizeRole(string role)
    {
        if (role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase))
        {
            return AdminRole;
        }

        if (role.Equals(MemberRole, StringComparison.OrdinalIgnoreCase))
        {
            return MemberRole;
        }

        if (role.Equals(VisitorRole, StringComparison.OrdinalIgnoreCase))
        {
            return VisitorRole;
        }

        throw new ConflictException("Role must be Admin, Member, or Visitor.");
    }

    private static void EnsureProjectTeammatesRoleColumn(MySqlConnection connection)
    {
        const string alterScript = """
            ALTER TABLE `ProjectTeammates`
            ADD COLUMN `Role` VARCHAR(32) NOT NULL DEFAULT 'Member';
            """;

        using var command = new MySqlCommand(alterScript, connection);
        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException exception) when (exception.Number == 1060)
        {
        }
    }
}
