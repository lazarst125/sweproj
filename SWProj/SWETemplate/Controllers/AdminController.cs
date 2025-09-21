using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWETemplate.Models;
using Microsoft.AspNetCore.Authorization;
using SWETemplate.DTOs; // Added for DTOs
using SWETemplate.Services; // Added for service

[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Admin,SuperAdmin")]
//ovo dole treba da se ukljuci tako da bi se prijavili kao admin sad i admin i donor mogu da vrse izmene a treba samo admin
// u tom slucaju ovo dole ne treba da bude komentarisano
// [Authorize(Roles = "Admin")] // Samo admini mogu da koriste ovaj kontroler
public class AdminController : ControllerBase
{
    private readonly SweContext _context;
    private readonly IAdminService _adminService; //  Added service

    public AdminController(SweContext context, IAdminService adminService)
    {
        _context = context;
        _adminService = adminService;
    }

    // ===========================
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

    //Novi endpoint za promovisanje korisnika u admina
    [HttpPost("Promote to admin/{id}/promote-to-admin")]
    //[Authorize(Roles = "SuperAdmin")] // ✅ CHECKED: Samo SuperAdmin može
    public async Task<IActionResult> PromoteToAdmin(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound("User nije pronađen.");

        // Sačuvano tvoj komentar/pravilo: korisnik će postati admin
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

    //CHECKED: Novi endpoint za demotovanje admina
    [HttpPost("Demote admin/{id}/demote-from-admin")]
    //[Authorize(Roles = "SuperAdmin")] // CHECKED: Samo SuperAdmin može
    public async Task<IActionResult> DemoteFromAdmin(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound("User nije pronađen.");

        // CHECKED: SuperAdmin ne može da demotuje samog sebe
        if (user.IsSuperAdmin)
            return BadRequest("Ne možete demotovati SuperAdmina.");

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

    // ===========================
    // UPDATE korisnika sa automatskim ažuriranjem Donor/Admin tabela
    // [HttpPut("users/{id}")]
    // [Tags("Users")]
    // public async Task<IActionResult> UpdateUser(int id, User updatedUser)
    // {
    //     var user = await _context.Users.FindAsync(id);
    //     if (user == null) return NotFound("User nije pronađen.");

    //     var oldRole = user.Role;
    //     user.Email = updatedUser.Email;
    //     user.Role = updatedUser.Role;

    //     // Ako je role promenjena
    //     if (oldRole != updatedUser.Role)
    //     {
    //         if (updatedUser.Role == "Admin")
    //         {
    //             // Briše iz Donors ako postoji
    //             var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
    //             if (donor != null) _context.Donors.Remove(donor);

    //             // Dodaje u Admins ako ne postoji
    //             var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
    //             if (admin == null)
    //             {
    //                 _context.Admins.Add(new Admin { UserId = id });
    //             }
    //         }
    //         else if (updatedUser.Role == "Donor")
    //         {
    //             // Briše iz Admins ako postoji
    //             var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
    //             if (admin != null) _context.Admins.Remove(admin);

    //             // Dodaje u Donors ako ne postoji
    //             var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
    //             if (donor == null)
    //             {
    //                 _context.Donors.Add(new Donor
    //                 {
    //                     UserId = id,
    //                     FirstName = "Unknown",
    //                     LastName = "Unknown",
    //                     BloodType = "",
    //                     Points = 0,
    //                     CanDonate = true,
    //                     DateOfBirth = DateTime.UtcNow,
    //                     PhoneNumber = "",
    //                     Address = "",
    //                     City = ""
    //                 });
    //             }
    //         }
    //     }

    //     await _context.SaveChangesAsync();
    //     return Ok(user);
    // }
    
    [HttpPut("Update user/{id}")]
    [Tags("Users")]
    public async Task<IActionResult> UpdateUser(int id, User updatedUser)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound("User nije pronađen.");

        // ✅ PROVERA: Da li korisnik koga ažuriramo jeste SuperAdmin
        if (user.IsSuperAdmin)
        {
            // ✅ Sprečavamo promenu uloge SuperAdminu
            if (user.Role != updatedUser.Role)
                return BadRequest("Ne možete promeniti ulogu SuperAdmina.");

            // ✅ Dozvoljavamo ažuriranje emaila ali ne i role
            user.Email = updatedUser.Email;
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        // Mapiraj na DTO i pozovi servis (logika servis)
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

    // ===========================
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

    // ===========================
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

    // ===========================
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

    // ===========================
    // // DELETE donora
    // [HttpDelete("donors/{id}")]
    // [Tags("Donors")]
    // public async Task<IActionResult> DeleteDonor(int id)
    // {
    //     var donor = await _context.Donors.FindAsync(id);
    //     if (donor == null) return NotFound("Donor nije pronađen.");

    //     var user = await _context.Users.FindAsync(donor.UserId);
    //     if (user != null && user.IsSuperAdmin)
    //         return BadRequest("Ne možete obrisati SuperAdmina.");

    //     _context.Donors.Remove(donor);

    //     // opcionalno: obriši i korisnika
    //     var _user = await _context.Users.FindAsync(donor.UserId);
    //     if (_user != null) _context.Users.Remove(_user);

    //     await _context.SaveChangesAsync();
    //     return Ok("Donor obrisan.");
    // }

    [HttpDelete("Delete donor/{id}")]
    [Tags("Donors")]
    public async Task<IActionResult> DeleteDonor(int id)
    {
        var donor = await _context.Donors.FindAsync(id);
        if (donor == null) return NotFound("Donor nije pronađen.");

        // Proveri da li je korisnik SuperAdmin ili Admin
        var user = await _context.Users
            .Include(u => u.AdminProfile) // Uključimo i admin profil ako postoji
            .FirstOrDefaultAsync(u => u.Id == donor.UserId);

        if (user == null)//slucaj kada imamo donor profil, ali ne postoji user koji mu odgovara
        { // Ovo bi trebalo da bude nemoguce ako su foreign key constraint-i postavljeni
            _context.Donors.Remove(donor);
            await _context.SaveChangesAsync();
            return Ok("Donor obrisan.");
        }

        // Onemogući brisanje SuperAdmina
        if (user.IsSuperAdmin)
            return BadRequest("Ne možete obrisati SuperAdmina.");

        // Obriši donor profil
        _context.Donors.Remove(donor);

        // Ako korisnik ima i admin profil, obriši i njega
        if (user.AdminProfile != null)
        {
            _context.Admins.Remove(user.AdminProfile);
        }

        // Obriši korisnika samo ako nema drugih profila
        // (U ovom slučaju, pošto smo obrisali i donor i admin profil, možemo obrisati i usera)
        _context.Users.Remove(user);

        await _context.SaveChangesAsync();
        return Ok("Donor obrisan.");
    }

    // // DELETE korisnika
    // [HttpDelete("users/{id}")]
    // [Tags("Users")]
    // public async Task<IActionResult> DeleteUser(int id)
    // {
    //     var user = await _context.Users.FindAsync(id);
    //     if (user == null) return NotFound("User nije pronađen.");

    //     // obriši povezane donore ili admine
    //     var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
    //     if (donor != null) _context.Donors.Remove(donor);

    //     var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
    //     if (admin != null) _context.Admins.Remove(admin);

    //     _context.Users.Remove(user);
    //     await _context.SaveChangesAsync();
    //     return Ok("User obrisan.");
    // }

    [HttpDelete("Delete user/{id}")]
    [Tags("Users")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.DonorProfile)
            .Include(u => u.AdminProfile)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound("User nije pronađen.");

        // ✅ CHECKED: Onemogući brisanje SuperAdmina
        if (user.IsSuperAdmin)
            return BadRequest("Ne možete obrisati SuperAdmina.");

        // ✅ CHECKED: Obriši povezane profile
        if (user.DonorProfile != null)
            _context.Donors.Remove(user.DonorProfile);

        if (user.AdminProfile != null)
            _context.Admins.Remove(user.AdminProfile);

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok("User obrisan.");
    }

    [HttpPost("Give superadmin/{id}")]
    [Tags("Superadmin")]
    public async Task<IActionResult> TransferSuperAdmin(int id)
    {
        //Pronađi trenutnog SuperAdmina
        var currentSuperAdmin = await _context.Users
            .FirstOrDefaultAsync(u => u.IsSuperAdmin);

        if (currentSuperAdmin == null)
            return NotFound("Trenutni SuperAdmin nije pronađen.");

        // Proveri da li target korisnik postoji
        var targetUser = await _context.Users.FindAsync(id);
        if (targetUser == null)
            return NotFound("Korisnik kome zelis da das privilegiju superadmina nije pronađen.");

        // Proveri da li je target korisnik već SuperAdmin
        if (targetUser.IsSuperAdmin)
            return BadRequest("Korisnik je vec SuperAdmin.");

        // Proveri da li je target korisnik admin
        if (targetUser.Role != "Admin")
            return BadRequest("Samo admini mogu postati SuperAdmin.");

        // Ukloni SuperAdmin status od trenutnog SuperAdmina
        currentSuperAdmin.IsSuperAdmin = false;
        currentSuperAdmin.Role = "Admin"; // Vrati ulogu na Admin

        // Dodeli SuperAdmin status target korisniku
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
    // private async Task<bool> IsCurrentUserSuperAdmin(int id)
    // {
    //     // Provera da li je trenutni korisnik SuperAdmin
    //     var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    //     if (userIdClaim == null) return false;

    //     var userId = int.Parse(userIdClaim.Value);
    //     var user = await _context.Users.FindAsync(userId);

    //     return user?.IsSuperAdmin ?? false;
    // }
}
