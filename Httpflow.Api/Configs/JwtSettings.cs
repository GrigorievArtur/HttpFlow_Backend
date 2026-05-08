namespace Httpflow.Api.Services.Auth;

public sealed record JwtSettings(
    string Issuer,
    string Audience,
    string Key,
    int ExpirationMinutes);
