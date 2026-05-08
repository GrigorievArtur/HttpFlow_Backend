using Httpflow.Api.Dtos.Users;
using Httpflow.Api.Infrastructure.Exceptions;
using Httpflow.Api.Services.Users;

namespace Httpflow.Api.Services.Auth;

public sealed class AuthService
{
    private readonly UserService _userService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(UserService userService, JwtTokenService jwtTokenService)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
    }

    public AuthResponseDto Register(RegisterUserDto dto)
    {
        var user = _userService.CreateUser(dto);
        return _jwtTokenService.CreateAuthResponse(user);
    }

    public AuthResponseDto Login(LoginUserDto dto)
    {
        string passwordHash;
        try
        {
            passwordHash = _userService.GetPasswordHashByEmail(dto.Email);
        }
        catch (ResourceNotFoundException)
        {
            throw new UnauthorizedApiException("Invalid email or password.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, passwordHash))
        {
            throw new UnauthorizedApiException("Invalid email or password.");
        }

        var user = _userService.GetUserByEmail(dto.Email);
        return _jwtTokenService.CreateAuthResponse(user);
    }
}
