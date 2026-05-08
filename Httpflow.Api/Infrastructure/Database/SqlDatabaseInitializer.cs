using MySqlConnector;

namespace Httpflow.Api.Infrastructure.Database;

public sealed class SqlDatabaseInitializer
{
    private readonly string _connectionString;
    private readonly bool _shouldDropAndCreate;
    private readonly string _scriptsDirectoryPath;

    public SqlDatabaseInitializer(string? contentRootPath = null)
    {
        _shouldDropAndCreate = bool.TryParse(GetRequiredEnv("DB_DROP_CREATE"), out var dropAndCreate) && dropAndCreate;

        _connectionString = Environment.GetEnvironmentVariable("SQL_DATABASE_URL")
            ?? BuildConnectionStringFromParts();

        var basePath = contentRootPath ?? AppContext.BaseDirectory;
        _scriptsDirectoryPath = Path.Combine(basePath, "Infrastructure", "Database", "Scripts");
    }

    public void RebuildSchema()
    {
        if (!_shouldDropAndCreate)
        {
            return;
        }

        var scriptFileNames = new[] { "drop.sql", "create.sql" };
        var scripts = new string[scriptFileNames.Length];

        for (var i = 0; i < scriptFileNames.Length; i++)
        {
            var scriptPath = Path.Combine(_scriptsDirectoryPath, scriptFileNames[i]);
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Schema SQL file not found: {scriptPath}");
            }

            scripts[i] = File.ReadAllText(scriptPath);
            if (string.IsNullOrWhiteSpace(scripts[i]))
            {
                throw new InvalidOperationException($"Schema SQL file is empty: {scriptPath}");
            }
        }

        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        foreach (var script in scripts)
        {
            using var command = new MySqlCommand(script, connection);
            command.ExecuteNonQuery();
        }
    }

    private static string GetRequiredEnv(string key)
    {
        return Environment.GetEnvironmentVariable(key)
            ?? throw new InvalidOperationException($"Missing required environment variable: {key}");
    }

    private static string BuildConnectionStringFromParts()
    {
        var host = GetRequiredEnv("DB_HOST");
        var port = GetRequiredEnv("DB_PORT");
        var database = GetRequiredEnv("DB_NAME");
        var user = GetRequiredEnv("DB_USER");
        var password = GetRequiredEnv("DB_PASSWORD");

        return $"Server={host};Port={port};Database={database};User ID={user};Password={password};";
    }
}
