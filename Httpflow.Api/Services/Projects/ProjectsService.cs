using Httpflow.Api.Dtos.Projects;
using Httpflow.Api.Infrastructure.Exceptions;
using MySqlConnector;

namespace Httpflow.Api.Services.Projects;

public sealed class ProjectsService
{
    private const string AdminRole = "Admin";
    private const string MemberRole = "Member";

    private readonly string _connectionString;

    public ProjectsService()
    {
        _connectionString = Environment.GetEnvironmentVariable("SQL_DATABASE_URL")
                            ?? throw new InvalidOperationException("Missing required environment variable: SQL_DATABASE_URL");
    }
    
    public ProjectDto CreateProject(int ownerUserId, CreateProjectDto dto)
    {
        const string insertScript = """
            INSERT INTO `Projects` (`OwnerUserId`, `Name`, `Value`)
            VALUES (@ownerUserId, @name, @value);
            """;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(insertScript, connection);
        command.Parameters.AddWithValue("@ownerUserId", ownerUserId);
        command.Parameters.AddWithValue("@name", dto.Name);
        command.Parameters.AddWithValue("@value", dto.Value);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ConflictException($"A project named {dto.Name} already exists for this user.");
        }

        return GetProjectById((int)command.LastInsertedId, connection);
    }

    
    public List<ProjectDto> GetPagedProjects(int ownerUserId, int pageNumber = 1, int pageSize = 5)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize < 1 ? 5 : pageSize;

        var offset = (pageNumber - 1) * pageSize;

        const string selectScript = """
                                    SELECT *
                                    FROM (
                                        SELECT p.`Id`, p.`OwnerUserId`, p.`Name`, p.`Value`
                                        FROM `Projects` p
                                        WHERE p.`OwnerUserId` = @userId
                                    
                                        UNION
                                    
                                        SELECT p.`Id`, p.`OwnerUserId`, p.`Name`, p.`Value`
                                        FROM `Projects` p
                                        WHERE p.`Id` IN (
                                            SELECT pt.`ProjectId`
                                            FROM `ProjectTeammates` pt
                                            WHERE pt.`UserId` = @userId
                                        )
                                    ) AS projects
                                    ORDER BY `Id`
                                    LIMIT @pageSize OFFSET @offset;
                                    """;
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        
        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@offset", offset);
        command.Parameters.AddWithValue("@pageSize", pageSize);
        command.Parameters.AddWithValue("@userId", ownerUserId);

        using var reader = command.ExecuteReader();
        var projects = new List<ProjectDto>();

        while (reader.Read())
        {
            projects.Add(new ProjectDto(
                reader.GetInt32("Id"),
                reader.GetInt32("OwnerUserId"),
                reader.GetString("Name"),
                reader.GetString("Value")));
        }

        return projects;
    }

    public ProjectDto GetProjectById(int projectId, int currentUserId)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        EnsureProjectAccess(projectId, currentUserId, connection);

        return GetProjectById(projectId, connection);
    }

    public ProjectDto UpdateProject(int projectId, int currentUserId, UpdateProjectDto dto)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        EnsureCanEditProject(projectId, currentUserId, connection);

        const string updateScript = """
            UPDATE `Projects`
            SET `Name` = @name,
                `Value` = @value
            WHERE `Id` = @projectId;
            """;

        using var command = new MySqlCommand(updateScript, connection);
        command.Parameters.AddWithValue("@projectId", projectId);
        command.Parameters.AddWithValue("@name", dto.Name);
        command.Parameters.AddWithValue("@value", dto.Value);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ConflictException($"A project named {dto.Name} already exists for this user.");
        }

        return GetProjectById(projectId, connection);
    }

    public ProjectDto DeleteProject(int projectId, int currentUserId)
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();
        EnsureCanDeleteProject(projectId, currentUserId, connection);

        var project = GetProjectById(projectId, connection);

        const string deleteScript = """
            DELETE FROM `Projects`
            WHERE `Id` = @projectId;
            """;

        using var command = new MySqlCommand(deleteScript, connection);
        command.Parameters.AddWithValue("@projectId", projectId);
        command.ExecuteNonQuery();

        return project;
    }

    private static ProjectDto GetProjectById(int id, MySqlConnection connection)
    {
        const string selectScript = """
            SELECT `Id`, `OwnerUserId`, `Name`, `Value`
            FROM `Projects`
            WHERE `Id` = @id
            LIMIT 1;
            """;

        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new ResourceNotFoundException($"Project with id {id} was not found.");
        }

        return new ProjectDto(
            reader.GetInt32("Id"),
            reader.GetInt32("OwnerUserId"),
            reader.GetString("Name"),
            reader.GetString("Value"));
    }

    private static void EnsureProjectAccess(int projectId, int userId, MySqlConnection connection)
    {
        if (GetProjectRole(projectId, userId, connection) is null)
        {
            throw new ForbiddenApiException("You do not have access to this project.");
        }
    }

    private static void EnsureCanEditProject(int projectId, int userId, MySqlConnection connection)
    {
        var role = GetProjectRole(projectId, userId, connection);
        if (role is not AdminRole and not MemberRole)
        {
            throw new ForbiddenApiException("You do not have permission to edit this project.");
        }
    }

    private static void EnsureCanDeleteProject(int projectId, int userId, MySqlConnection connection)
    {
        if (GetProjectRole(projectId, userId, connection) != AdminRole)
        {
            throw new ForbiddenApiException("Only project admins can delete this project.");
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
        return string.IsNullOrWhiteSpace(role) ? null : role;
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
}
