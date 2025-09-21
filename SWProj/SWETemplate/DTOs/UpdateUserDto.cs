using System.ComponentModel.DataAnnotations;
namespace SWETemplate.DTOs
{
    public class UpdateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [RegularExpression("Admin|Donor", ErrorMessage = "Role must be Admin or Donor")]
        public string Role { get; set; }
    }
}