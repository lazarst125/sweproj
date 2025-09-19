namespace SWETemplate.DTOs;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Token { get; set; } = string.Empty;
}