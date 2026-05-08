using Httpflow.Api.Dtos.Users;
using Httpflow.Api.Infrastructure.Exceptions;
using MySqlConnector;

namespace Httpflow.Api.Services.Users;

public sealed class UserService
{
    private readonly string _connectionString;

    public UserService()
    {
        _connectionString = Environment.GetEnvironmentVariable("SQL_DATABASE_URL")
            ?? throw new InvalidOperationException("Missing required environment variable: SQL_DATABASE_URL");
    }

    public UserDto CreateUser(RegisterUserDto dto)
    {
        const string insertScript = """
            INSERT INTO `Users` (`Firstname`, `Lastname`, `Email`, `Password`)
            VALUES (@firstname, @lastname, @email, @password);
            """;
        const string selectScript = """
            SELECT `Id`, `Firstname`, `Lastname`, `Email`
            FROM `Users`
            WHERE `Id` = @id;
            """;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(insertScript, connection);
        command.Parameters.AddWithValue("@firstname", dto.Firstname);
        command.Parameters.AddWithValue("@lastname", dto.Lastname);
        command.Parameters.AddWithValue("@email", dto.Email);
        command.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(dto.Password));

        try
        {
            command.ExecuteNonQuery();
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ConflictException($"A user with email {dto.Email} already exists.");
        }

        var userId = (int)command.LastInsertedId;
        return GetUserById(userId, connection, selectScript);
    }

    public IReadOnlyList<UserDto> GetUsers()
    {
        const string selectScript = """
            SELECT `Id`, `Firstname`, `Lastname`, `Email`
            FROM `Users`
            ORDER BY `Id`;
            """;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(selectScript, connection);
        using var reader = command.ExecuteReader();

        var users = new List<UserDto>();
        while (reader.Read())
        {
            users.Add(new UserDto(
                reader.GetInt32("Id"),
                reader.GetString("Firstname"),
                reader.GetString("Lastname"),
                reader.GetString("Email")));
        }

        return users;
    }

    public UserDto GetUserByEmail(string email)
    {
        const string selectScript = """
            SELECT `Id`, `Firstname`, `Lastname`, `Email`
            FROM `Users`
            WHERE `Email` = @email
            LIMIT 1;
            """;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@email", email);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new ResourceNotFoundException($"User with email {email} was not found.");
        }

        return new UserDto(
            reader.GetInt32("Id"),
            reader.GetString("Firstname"),
            reader.GetString("Lastname"),
            reader.GetString("Email"));
    }
    
    public string GetPasswordHashByEmail(string email)
    {
        const string selectScript = """
            SELECT `Password`
            FROM `Users`
            WHERE `Email` = @email
            LIMIT 1;
            """;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@email", email);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new ResourceNotFoundException($"User with email {email} was not found.");
        }

        return reader.GetString("Password");
    }

    public UserDto GetUserById(int id)
    {
        const string selectScript = """
            SELECT `Id`, `Firstname`, `Lastname`, `Email`
            FROM `Users`
            WHERE `Id` = @id
            LIMIT 1;
            """;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        return GetUserById(id, connection, selectScript);
    }

    public UserDto UpdateUser(int id, UpdateUserDto dto)
    {
        const string updateScript = """
            UPDATE `Users`
            SET `Firstname` = @firstname,
                `Lastname` = @lastname,
                `Email` = @email
            WHERE `Id` = @id;
            """;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(updateScript, connection);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@firstname", dto.Firstname);
        command.Parameters.AddWithValue("@lastname", dto.Lastname);
        command.Parameters.AddWithValue("@email", dto.Email);

        try
        {
            var affectedRows = command.ExecuteNonQuery();
            if (affectedRows == 0)
            {
                throw new ResourceNotFoundException($"User with id {id} was not found.");
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new ConflictException($"A user with email {dto.Email} already exists.");
        }

        return GetUserById(id, connection, """
            SELECT `Id`, `Firstname`, `Lastname`, `Email`
            FROM `Users`
            WHERE `Id` = @id
            LIMIT 1;
            """);
    }

    public void DeleteUser(int id)
    {
        const string deleteScript = """
            DELETE FROM `Users`
            WHERE `Id` = @id;
            """;

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = new MySqlCommand(deleteScript, connection);
        command.Parameters.AddWithValue("@id", id);
        if (command.ExecuteNonQuery() == 0)
        {
            throw new ResourceNotFoundException($"User with id {id} was not found.");
        }
    }

    private static UserDto GetUserById(int id, MySqlConnection connection, string selectScript)
    {
        using var command = new MySqlCommand(selectScript, connection);
        command.Parameters.AddWithValue("@id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new ResourceNotFoundException($"User with id {id} was not found.");
        }

        return new UserDto(
            reader.GetInt32("Id"),
            reader.GetString("Firstname"),
            reader.GetString("Lastname"),
            reader.GetString("Email"));
    }
}
