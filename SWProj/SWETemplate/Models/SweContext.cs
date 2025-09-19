using Microsoft.EntityFrameworkCore;

namespace SWETemplate.Models;

public class SweContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Donor> Donors { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<BloodDonationEvent> BloodDonationEvents { get; set; }
    public DbSet<Donation> Donations { get; set; }
    public DbSet<BloodInventory> BloodInventories { get; set; }

    public SweContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Donor>()
            .HasOne(d => d.User)
            .WithOne()
            .HasForeignKey<Donor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Admin>()
            .HasOne(a => a.User)
            .WithOne()
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
