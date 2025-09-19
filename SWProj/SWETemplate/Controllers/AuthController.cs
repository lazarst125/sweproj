using Microsoft.AspNetCore.Mvc;
using SWETemplate.DTOs;
using SWETemplate.Services;
using Microsoft.Extensions.Logging;

namespace SWETemplate.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            _logger.LogInformation("Register attempt for email: {Email}", registerDto.Email);
            
            // Provera model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for registration");
                return BadRequest(new { message = "Invalid data", errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            var result = await _authService.Register(registerDto);
            _logger.LogInformation("Registration successful for email: {Email}", registerDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", registerDto.Email);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);
            
            // Provera model state
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for login");
                return BadRequest(new { message = "Invalid data", errors = ModelState.Values.SelectMany(v => v.Errors) });
            }

            var result = await _authService.Login(loginDto);
            _logger.LogInformation("Login successful for email: {Email}", loginDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
            return Unauthorized(new { message = ex.Message });
        }
    }
}