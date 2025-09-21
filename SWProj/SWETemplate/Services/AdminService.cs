using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SWETemplate.DTOs;
using SWETemplate.Models;

namespace SWETemplate.Services
{
    public class AdminService : IAdminService
    {
        private readonly SweContext _context;

        public AdminService(SweContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Include(u => u.DonorProfile)
                .Include(u => u.AdminProfile)
                .ToListAsync();

            return users.Select(u => MapToDto(u));
        }

        public async Task PromoteToAdminAsync(int userId)
        {
            // Servis baca izuzetke za poslovna pravila.
            var user = await _context.Users
                .Include(u => u.DonorProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User nije pronađen."); // CHECKED: servis validira i signalizira ako user ne postoji

            if (user.Role == "Admin" || user.IsSuperAdmin)
                throw new InvalidOperationException("Korisnik je već Admin ili SuperAdmin."); // CHECKED: vraćamo BadRequest iz kontrolera

            user.Role = "Admin";

            var adminProfile = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
            if (adminProfile == null)
            {
                _context.Admins.Add(new Admin
                {
                    UserId = userId,
                    FirstName = user.DonorProfile?.FirstName ?? "Admin", // CHECKED: koristimo fallback
                    LastName = user.DonorProfile?.LastName ?? "User"     // CHECKED: koristimo fallback
                });
            }

            // CHECKED: Ostavili smo donor profil (admin moze biti i donor) — ne brisemo donor profile prilikom promocije.
            // prethodno su postojale linije koje su uklanjale donor profil; sada su uklonjene.

            await _context.SaveChangesAsync();
        }

        public async Task DemoteFromAdminAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.AdminProfile)
                .Include(u => u.DonorProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User nije pronađen."); // CHECKED

            if (user.IsSuperAdmin)
                throw new InvalidOperationException("Ne možete demotovati SuperAdmina."); // CHECKED

            if (user.Role != "Admin")
                return; // nije admin, nema sta da se radi

            user.Role = "Donor";

            var adminProfile = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
            if (adminProfile != null)
                _context.Admins.Remove(adminProfile);

            // Ako nema donor profila, dodajemo ga (zadržavamo podatke ako postoje)
            var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (donor == null)
            {
                _context.Donors.Add(new Donor
                {
                    UserId = userId,
                    FirstName = user.AdminProfile?.FirstName ?? "Unknown", // CHECKED: fallback
                    LastName = user.AdminProfile?.LastName ?? "Unknown",
                    BloodType = string.Empty,
                    Points = 0,
                    CanDonate = true,
                    DateOfBirth = DateTime.UtcNow,
                    PhoneNumber = string.Empty,
                    Address = string.Empty,
                    City = string.Empty
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<UserResponseDto> UpdateUserAsync(int id, UpdateUserDto updateDto)
        {
            var user = await _context.Users
                .Include(u => u.DonorProfile)
                .Include(u => u.AdminProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                throw new InvalidOperationException("User nije pronađen."); // CHECKED

            // CHECKED: Ne dozvoljavamo menjanje role SuperAdmin-a
            if (user.IsSuperAdmin && user.Role != updateDto.Role)
                throw new InvalidOperationException("Ne možete promeniti ulogu SuperAdmina.");

            var oldRole = user.Role;
            user.Email = updateDto.Email;
            user.Role = updateDto.Role;

            if (oldRole != updateDto.Role)
            {
                if (updateDto.Role == "Admin")
                {
                    // Dodaj admin profil ako ne postoji, ne brisemo donor profil (admin moze biti i donor)
                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
                    if (admin == null)
                    {
                        _context.Admins.Add(new Admin
                        {
                            UserId = id,
                            FirstName = user.DonorProfile?.FirstName ?? "Admin", // CHECKED: null-safe
                            LastName = user.DonorProfile?.LastName ?? "User"
                        });
                    }
                }
                else if (updateDto.Role == "Donor")
                {
                    // Ako prelazi na Donor ukloniti Admin profil ako postoji
                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
                    if (admin != null) _context.Admins.Remove(admin);

                    // Dodaj donor profil ako ne postoji
                    var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
                    if (donor == null)
                    {
                        _context.Donors.Add(new Donor
                        {
                            UserId = id,
                            FirstName = user.AdminProfile?.FirstName ?? "Unknown",
                            LastName = user.AdminProfile?.LastName ?? "Unknown",
                            BloodType = string.Empty,
                            Points = 0,
                            CanDonate = true,
                            DateOfBirth = DateTime.UtcNow,
                            PhoneNumber = string.Empty,
                            Address = string.Empty,
                            City = string.Empty
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return MapToDto(user);
        }

        public async Task DeleteDonorAsync(int donorId)
        {
            var donor = await _context.Donors.FindAsync(donorId);
            if (donor == null)
                throw new InvalidOperationException("Donor nije pronađen."); // CHECKED

            var user = await _context.Users
                .Include(u => u.AdminProfile)
                .FirstOrDefaultAsync(u => u.Id == donor.UserId);

            if (user == null)
            {
                // Ako nema user-a, samo obriši donor profil (treba da bude retko)
                _context.Donors.Remove(donor);
                await _context.SaveChangesAsync();
                return;
            }

            if (user.IsSuperAdmin)
                throw new InvalidOperationException("Ne možete obrisati SuperAdmina."); // CHECKED

            // Ukloni samo donor profil
            _context.Donors.Remove(donor);

            // Ako user nije admin (nema adminProfile), obriši i korisnika
            if (user.AdminProfile == null)
            {
                _context.Users.Remove(user); // CHECKED: brisemo user-a samo ako nije admin
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.DonorProfile)
                .Include(u => u.AdminProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException("User nije pronađen."); // CHECKED

            if (user.IsSuperAdmin)
                throw new InvalidOperationException("Ne možete obrisati SuperAdmina."); // CHECKED

            // Obriši profile (ako postoje)
            if (user.DonorProfile != null) _context.Donors.Remove(user.DonorProfile);
            if (user.AdminProfile != null) _context.Admins.Remove(user.AdminProfile);

            // Obriši user-a
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
        }

        public async Task TransferSuperAdminAsync(int toUserId)
        {
            var currentSuperAdmin = await _context.Users.FirstOrDefaultAsync(u => u.IsSuperAdmin);
            if (currentSuperAdmin == null)
                throw new InvalidOperationException("Trenutni SuperAdmin nije pronađen."); // CHECKED

            var targetUser = await _context.Users.FindAsync(toUserId);
            if (targetUser == null)
                throw new InvalidOperationException("Korisnik kome zelis da das privilegiju superadmina nije pronađen."); // CHECKED

            if (targetUser.IsSuperAdmin)
                throw new InvalidOperationException("Korisnik je vec SuperAdmin."); // CHECKED

            if (targetUser.Role != "Admin")
                throw new InvalidOperationException("Samo admini mogu postati SuperAdmin."); // CHECKED

            currentSuperAdmin.IsSuperAdmin = false;
            currentSuperAdmin.Role = "Admin";

            targetUser.IsSuperAdmin = true;
            targetUser.Role = "SuperAdmin";

            await _context.SaveChangesAsync();
        }

        // Helper to map User -> UserResponseDto
        private UserResponseDto MapToDto(User u)
        {
            return new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                FirstName = u.DonorProfile?.FirstName ?? u.AdminProfile?.FirstName ?? string.Empty,
                LastName = u.DonorProfile?.LastName ?? u.AdminProfile?.LastName ?? string.Empty,
                BloodType = u.DonorProfile?.BloodType ?? string.Empty,
                Points = u.DonorProfile?.Points ?? 0
            };
        }
    }
}
