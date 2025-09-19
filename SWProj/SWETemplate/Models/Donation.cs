namespace SWETemplate.Models;

public class Donation
{
    public int Id { get; set; }
    public int DonorId { get; set; }
    public int EventId { get; set; }
    public DateTime DonationDate { get; set; } = DateTime.UtcNow;
    public string BloodType { get; set; } = string.Empty;
    public int Quantity { get; set; } = 450;
    public bool IsProcessed { get; set; } = false;
    
    public Donor? Donor { get; set; }
    public BloodDonationEvent? Event { get; set; }
}