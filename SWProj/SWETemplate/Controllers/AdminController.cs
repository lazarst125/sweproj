using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWETemplate.Models;
using Microsoft.AspNetCore.Authorization;
using SWETemplate.DTOs;
using SWETemplate.Services;
using System.Security.Claims; // Added for Claims

//TREBA DA DODAMO ODGOVARAJUCE AUTHORIZE(ROLES="...") za fje tako da se postuje hijerarhija uloga

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")] // CHECKED: Only Admin and SuperAdmin can access
public class AdminController : ControllerBase
{
    private readonly SweContext _context;
    private readonly IAdminService _adminService;

    public AdminController(SweContext context, IAdminService adminService)
    {
        _context = context;
        _adminService = adminService;
    }

    // GET: svi donori
    [HttpGet("Return donors")]
    [Tags("Donors")]
    public async Task<IActionResult> GetDonors()
    {
        var donors = await _context.Donors.Include(d => d.User).ToListAsync();
        return Ok(donors);
    }

    // GET: svi korisnici
    [HttpGet("Return users")]
    [Tags("Users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _adminService.GetAllUsersAsync();
        return Ok(users);
    }

    // GET: sve donacije
    [HttpGet("Return donations")]
    [Tags("Donations")]
    public async Task<IActionResult> GetDonations()
    {
        var donations = await _context.Donations
            .Include(d => d.Donor)
            .Include(d => d.Event)
            .ToListAsync();
        return Ok(donations);
    }

    // Novi endpoint za promovisanje korisnika u admina
    [HttpPost("Promote to admin/{id}/promote-to-admin")]
    [Authorize(Roles = "SuperAdmin")] // ✅ CHECKED: Only SuperAdmin can promote
    public async Task<IActionResult> PromoteToAdmin(int id)
    {
        try
        {
            await _adminService.PromoteToAdminAsync(id);
            return Ok("Korisnik je sada admin.");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // Novi endpoint za demotovanje admina
    [HttpPost("Demote admin/{id}/demote-from-admin")]
    [Authorize(Roles = "SuperAdmin")] // ✅ CHECKED: Only SuperAdmin can demote
    public async Task<IActionResult> DemoteFromAdmin(int id)
    {
        try
        {
            await _adminService.DemoteFromAdminAsync(id);
            return Ok("Korisnik više nije admin.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("Update user/{id}")]
    [Tags("Users")]
    public async Task<IActionResult> UpdateUser(int id, User updatedUser)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound("User nije pronađen.");

        // ✅ CHECKED: Prevent changing SuperAdmin role
        if (user.IsSuperAdmin && user.Role != updatedUser.Role)
            return BadRequest("Ne možete promeniti ulogu SuperAdmina.");

        try
        {
            var updateDto = new UpdateUserDto
            {
                Email = updatedUser.Email,
                Role = updatedUser.Role
            };

            var result = await _adminService.UpdateUserAsync(id, updateDto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // UPDATE krvne grupe donora po ID-u
    [HttpPut("Update donor's bloodtype/{id}/bloodtype")]
    [Tags("Donors")]
    public async Task<IActionResult> UpdateDonorBloodType(int id, [FromBody] string newBloodType)
    {
        var donor = await _context.Donors.FindAsync(id);
        if (donor == null) return NotFound("Donor nije pronađen.");

        donor.BloodType = newBloodType;
        await _context.SaveChangesAsync();

        return Ok(donor);
    }

    // UPDATE donora (ostali podaci)
    [HttpPut("Update donor/{id}")]
    [Tags("Donors")]
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

    // UPDATE donacije
    [HttpPut("Update donations/{id}")]
    [Tags("Donations")]
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

    [HttpDelete("Delete donor/{id}")]
    [Tags("Donors")]
    public async Task<IActionResult> DeleteDonor(int id)
    {
        try
        {
            // Koristimo servis metodu umesto direktnog poziva
            await _adminService.DeleteDonorByIdAsync(id);
            return Ok("Donor obrisan.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("Delete user/{id}")]
    [Tags("Users")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            // ✅ CHECKED: Proveravamo da li je korisnik admin i da li trenutni korisnik može da ga obriše
            var user = await _context.Users
                .Include(u => u.AdminProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound("Korisnik nije pronađen.");

            if (user.IsSuperAdmin)
                return BadRequest("Ne možete obrisati SuperAdmina.");

            if (user.AdminProfile != null && !User.IsInRole("SuperAdmin"))
                return Forbid("Samo SuperAdmin može obrisati admina.");

            // ✅ CHECKED: Koristimo servis metodu za brisanje
            await _adminService.DeleteUserAsync(id);
            return Ok("Korisnik obrisan.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("Give superadmin/{id}")]
    [Tags("Superadmin")]
    [Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can transfer
    public async Task<IActionResult> TransferSuperAdmin(int id)
    {
        var currentSuperAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.IsSuperAdmin);

        if (currentSuperAdmin == null)
            return NotFound("Trenutni SuperAdmin nije pronađen.");

        var targetUser = await _context.Users.FindAsync(id);
        if (targetUser == null)
            return NotFound("Target korisnik nije pronađen.");

        if (targetUser.IsSuperAdmin)
            return BadRequest("Korisnik je već SuperAdmin.");

        if (targetUser.Role != "Admin")
            return BadRequest("Samo admini mogu postati SuperAdmin.");

        currentSuperAdmin.IsSuperAdmin = false;
        currentSuperAdmin.Role = "Admin";

        targetUser.IsSuperAdmin = true;
        targetUser.Role = "SuperAdmin";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "SuperAdmin privilegije uspešno prenete",
            PreviousSuperAdminId = currentSuperAdmin.Id,
            NewSuperAdminId = targetUser.Id
        });
    }

}