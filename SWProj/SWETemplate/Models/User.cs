namespace SWETemplate.Models;
using System.ComponentModel.DataAnnotations;

public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Email je obavezan.")]
    [EmailAddress(ErrorMessage = "Unesite validnu email adresu.")]
    public string Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Donor";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsSuperAdmin { get; set; } = false;//glavni admin

    public virtual Donor DonorProfile { get; set; }
    public virtual Admin AdminProfile { get; set; } 
}