using System.IO;
using ApiTestTool.Database.Common;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
var envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");

if (File.Exists(envFilePath))
{
    Env.Load(envFilePath);
}

new SQLDatabaseInitializer(builder.Environment.ContentRootPath).RebuildSchema();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
