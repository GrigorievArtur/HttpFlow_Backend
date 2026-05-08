using MySqlConnector;

namespace Httpflow.Api.Services.Users;

public class UserService
{
    private readonly string? _connectionString;

    public UserService()
    {
        _connectionString = Environment.GetEnvironmentVariable("SQL_DATABASE_URL");
    }
}
