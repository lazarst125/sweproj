using SWETemplate.DTOs;

namespace SWETemplate.Services;

public interface IAuthService
{
    Task<UserResponseDto> Register(RegisterDto registerDto);
    Task<UserResponseDto> Login(LoginDto loginDto);
    string GenerateJwtToken(User user);
}