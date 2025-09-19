using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWETemplate.Models;

namespace SWETemplate.Controllers;

[ApiController]
[Route("api/[controller]")]
//ovo dole treba da se ukljuci tako da bi se prijavili kao admin sad i admin i donor mogu da vrse izmene a treba samo admin
// u tom slucaju ovo dole ne treba da bude komentarisano
// [Authorize(Roles = "Admin")] // Samo admini mogu da koriste ovaj kontroler
public class AdminController : ControllerBase
{
    private readonly SweContext _context;

    public AdminController(SweContext context)
    {
        _context = context;
    }

    // ===========================
    // GET: svi donori
    [HttpGet("donors")]
    public async Task<IActionResult> GetDonors()
    {
        var donors = await _context.Donors.Include(d => d.User).ToListAsync();
        return Ok(donors);
    }

    // GET: svi korisnici
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    // GET: sve donacije
    [HttpGet("donations")]
    public async Task<IActionResult> GetDonations()
    {
        var donations = await _context.Donations
            .Include(d => d.Donor)
            .Include(d => d.Event)
            .ToListAsync();
        return Ok(donations);
    }

    // ===========================
    // UPDATE korisnika sa automatskim ažuriranjem Donor/Admin tabela
    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(int id, User updatedUser)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound("User nije pronađen.");

        var oldRole = user.Role;
        user.Email = updatedUser.Email;
        user.Role = updatedUser.Role;

        // Ako je role promenjena
        if (oldRole != updatedUser.Role)
        {
            if (updatedUser.Role == "Admin")
            {
                // Briše iz Donors ako postoji
                var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
                if (donor != null) _context.Donors.Remove(donor);

                // Dodaje u Admins ako ne postoji
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
                if (admin == null)
                {
                    _context.Admins.Add(new Admin { UserId = id });
                }
            }
            else if (updatedUser.Role == "Donor")
            {
                // Briše iz Admins ako postoji
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
                if (admin != null) _context.Admins.Remove(admin);

                // Dodaje u Donors ako ne postoji
                var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
                if (donor == null)
                {
                    _context.Donors.Add(new Donor
                    {
                        UserId = id,
                        FirstName = "Unknown",
                        LastName = "Unknown",
                        BloodType = "",
                        Points = 0,
                        CanDonate = true,
                        DateOfBirth = DateTime.UtcNow,
                        PhoneNumber = "",
                        Address = "",
                        City = ""
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(user);
    }

    // ===========================
    // UPDATE krvne grupe donora po ID-u
    [HttpPut("donors/{id}/bloodtype")]
    public async Task<IActionResult> UpdateDonorBloodType(int id, [FromBody] string newBloodType)
    {
        var donor = await _context.Donors.FindAsync(id);
        if (donor == null) return NotFound("Donor nije pronađen.");

        donor.BloodType = newBloodType;
        await _context.SaveChangesAsync();

        return Ok(donor);
    }

    // ===========================
    // UPDATE donora (ostali podaci)
    [HttpPut("donors/{id}")]
    public async Task<IActionResult> UpdateDonor(int id, Donor updatedDonor)
    {
        var donor = await _context.Donors.FindAsync(id);
        if (donor == null) return NotFound("Donor nije pronađen.");

        donor.FirstName = updatedDonor.FirstName;
        donor.LastName = updatedDonor.LastName;
        donor.BloodType = updatedDonor.BloodType;
        donor.DateOfBirth = updatedDonor.DateOfBirth;
        donor.PhoneNumber = updatedDonor.PhoneNumber;
        donor.Address = updatedDonor.Address;
        donor.City = updatedDonor.City;
        donor.Points = updatedDonor.Points;
        donor.CanDonate = updatedDonor.CanDonate;

        await _context.SaveChangesAsync();
        return Ok(donor);
    }

    // ===========================
    // UPDATE donacije
    [HttpPut("donations/{id}")]
    public async Task<IActionResult> UpdateDonation(int id, Donation updatedDonation)
    {
        var donation = await _context.Donations.FindAsync(id);
        if (donation == null) return NotFound("Donacija nije pronađena.");

        donation.DonorId = updatedDonation.DonorId;
        donation.EventId = updatedDonation.EventId;
        donation.DonationDate = updatedDonation.DonationDate;
        donation.BloodType = updatedDonation.BloodType;
        donation.Quantity = updatedDonation.Quantity;
        donation.IsProcessed = updatedDonation.IsProcessed;

        await _context.SaveChangesAsync();
        return Ok(donation);
    }

    // ===========================
    // DELETE donora
    [HttpDelete("donors/{id}")]
    public async Task<IActionResult> DeleteDonor(int id)
    {
        var donor = await _context.Donors.FindAsync(id);
        if (donor == null) return NotFound("Donor nije pronađen.");

        _context.Donors.Remove(donor);

        // opcionalno: obriši i korisnika
        var user = await _context.Users.FindAsync(donor.UserId);
        if (user != null) _context.Users.Remove(user);

        await _context.SaveChangesAsync();
        return Ok("Donor obrisan.");
    }

    // DELETE korisnika
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound("User nije pronađen.");

        // obriši povezane donore ili admine
        var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
        if (donor != null) _context.Donors.Remove(donor);

        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
        if (admin != null) _context.Admins.Remove(admin);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok("User obrisan.");
    }

    // DELETE donacije
    [HttpDelete("donations/{id}")]
    public async Task<IActionResult> DeleteDonation(int id)
    {
        var donation = await _context.Donations.FindAsync(id);
        if (donation == null) return NotFound("Donacija nije pronađena.");

        _context.Donations.Remove(donation);
        await _context.SaveChangesAsync();
        return Ok("Donacija obrisana.");
    }
}
