namespace SWETemplate.Models;

public class BloodInventory
{
    public int Id { get; set; }
    public string BloodType { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public int MinimumRequired { get; set; } = 20;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}