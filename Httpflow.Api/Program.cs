using System.IO;
using System.Text;
using DotNetEnv;
using Httpflow.Api.Infrastructure;
using Httpflow.Api.Infrastructure.Database;
using Httpflow.Api.Services.Auth;
using Httpflow.Api.Services.Projects;
using Httpflow.Api.Services.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");

if (File.Exists(envFilePath))
{
    Env.Load(envFilePath);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Httpflow API",
        Version = "v1",
        Description = "REST API for Httpflow."
    });

    const string jwtSchemeId = JwtBearerDefaults.AuthenticationScheme;

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste a JWT bearer token in the format: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition(jwtSchemeId, jwtSecurityScheme);
    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(jwtSchemeId, hostDocument: null, externalResource: null)] = []
    });
});

var jwtIssuer = GetRequiredEnv("JWT_ISSUER");
var jwtAudience = GetRequiredEnv("JWT_AUDIENCE");
var jwtKey = GetRequiredEnv("JWT_KEY");
var jwtSettings = new JwtSettings(
    jwtIssuer,
    jwtAudience,
    jwtKey,
    int.TryParse(GetRequiredEnv("JWT_EXPIRATION_MINUTES"), out var expirationMinutes)
        ? expirationMinutes
        : 60);

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectsService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

new SqlDatabaseInitializer(builder.Environment.ContentRootPath).RebuildSchema();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Httpflow API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.MapGet("/api/v1/health", () => Results.Ok(new
{
    status = "ok",
    service = "httpflow-api"
}));

app.Run();

static string GetRequiredEnv(string key)
{
    return Environment.GetEnvironmentVariable(key)
        ?? throw new InvalidOperationException($"Missing required environment variable: {key}");
}
