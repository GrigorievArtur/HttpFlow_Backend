using Httpflow.Api.Dtos.Projects;
using Httpflow.Api.Infrastructure.Exceptions;
using MySqlConnector;

namespace Httpflow.Api.Services.Projects;

public sealed class ProjectsService
{
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
}
