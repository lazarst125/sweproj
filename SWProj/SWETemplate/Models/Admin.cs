namespace SWETemplate.Models;

public class Admin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public virtual User? User { get; set; }
}
