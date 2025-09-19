namespace SWETemplate.Models;

public class BloodDonationEvent
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Organizer { get; set; } = string.Empty;
    public int RegisteredDonors { get; set; } = 0;
    public int MaxDonors { get; set; } = 50;
    public bool IsActive { get; set; } = true;
    
    public List<Donation> Donations { get; set; } = new List<Donation>();
}