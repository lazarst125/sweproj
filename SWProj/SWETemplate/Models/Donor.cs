namespace SWETemplate.Models;

public class Donor
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Points { get; set; } = 0;
    public DateTime? LastDonationDate { get; set; }
    public bool CanDonate { get; set; } = true;
    
    public User? User { get; set; }
    public List<Donation> Donations { get; set; } = new List<Donation>();
}