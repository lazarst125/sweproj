using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SWETemplate.DTOs;
using SWETemplate.Models;

namespace SWETemplate.Services;

public class AuthService : IAuthService
{
    private readonly SweContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(SweContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<UserResponseDto> Register(RegisterDto registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            throw new Exception("Korisnik sa ovim emailom već postoji.");
        }

        if (registerDto.Password != registerDto.ConfirmPassword)
        {
            throw new Exception("Šifre se ne poklapaju.");
        }

        if (registerDto.Role != "Admin" && registerDto.Role != "Donor")
        {
            throw new Exception("Role mora biti 'Admin' ili 'Donor'.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        var user = new User
        {
            Email = registerDto.Email,
            PasswordHash = passwordHash,
            Role = registerDto.Role,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (registerDto.Role == "Donor")
        {
            var donor = new Donor
            {
                UserId = user.Id,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                BloodType = registerDto.BloodType,
                DateOfBirth = registerDto.DateOfBirth,
                PhoneNumber = registerDto.PhoneNumber,
                Address = registerDto.Address,
                City = registerDto.City,
                Points = 0,
                CanDonate = true
            };
            _context.Donors.Add(donor);
        }
        else if (registerDto.Role == "Admin")
        {
            var admin = new Admin
            {
                UserId = user.Id,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName
            };
            _context.Admins.Add(admin);
        }

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            BloodType = registerDto.Role == "Donor" ? registerDto.BloodType : "",
            Points = registerDto.Role == "Donor" ? 0 : 0,
            Token = token
        };
    }

    public async Task<UserResponseDto> Login(LoginDto loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new Exception("Pogrešan email ili šifra.");
        }

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        string firstName = "";
        string lastName = "";
        string bloodType = "";
        int points = 0;

        if (user.Role == "Donor")
        {
            var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (donor != null)
            {
                firstName = donor.FirstName;
                lastName = donor.LastName;
                bloodType = donor.BloodType;
                points = donor.Points;
            }
        }
        else if (user.Role == "Admin")
        {
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (admin != null)
            {
                firstName = admin.FirstName;
                lastName = admin.LastName;
            }
        }

        var token = GenerateJwtToken(user);

        return new UserResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role,
            FirstName = firstName,
            LastName = lastName,
            BloodType = bloodType,
            Points = points,
            Token = token
        };
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? "FallbackKeyIfConfigurationIsMissing");

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
