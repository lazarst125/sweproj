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

            return users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                FirstName = u.DonorProfile?.FirstName ?? u.AdminProfile?.FirstName ?? string.Empty,
                LastName = u.DonorProfile?.LastName ?? u.AdminProfile?.LastName ?? string.Empty,
                BloodType = u.DonorProfile?.BloodType ?? string.Empty,
                Points = u.DonorProfile?.Points ?? 0
            });
        }

        public async Task PromoteToAdminAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new InvalidOperationException("User nije pronađen.");

            if (user.Role == "Admin" || user.IsSuperAdmin)
            {
                throw new InvalidOperationException("Korisnik je već Admin ili SuperAdmin.");
            }    // already admin/superadmin

            user.Role = "Admin";

            var adminProfile = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
            if (adminProfile == null)
            {
                _context.Admins.Add(new Admin
                {
                    UserId = userId,
                    FirstName = user.DonorProfile?.FirstName ?? "Admin",
                    LastName = user.DonorProfile?.LastName ?? "User"
                });
            }

            // zelimo da i admin moze da bude donor, pa brisem ovo - izmenjeno
            
            // var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == userId);
            // if (donor != null) _context.Donors.Remove(donor);

            await _context.SaveChangesAsync();
        }

        public async Task DemoteFromAdminAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new InvalidOperationException("User nije pronađen.");

            if (user.IsSuperAdmin)
                throw new InvalidOperationException("Ne možete demotovati SuperAdmina.");

            if (user.Role != "Admin") return; // not an admin

            user.Role = "Donor";

            var adminProfile = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == userId);
            if (adminProfile != null) _context.Admins.Remove(adminProfile);

            var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (donor == null)
            {
                _context.Donors.Add(new Donor
                {
                    UserId = userId,
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

            await _context.SaveChangesAsync();
        }

        public async Task<UserResponseDto> UpdateUserAsync(int id, UpdateUserDto updateDto)
        {
            var user = await _context.Users
                .Include(u => u.DonorProfile)
                .Include(u => u.AdminProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) throw new InvalidOperationException("User nije pronađen.");

            if (user.IsSuperAdmin)
            {
                if (user.Role != updateDto.Role)
                    throw new InvalidOperationException("Ne možete promeniti ulogu SuperAdmina.");

                user.Email = updateDto.Email;
                await _context.SaveChangesAsync();

                return MapToDto(user);
            }

            var oldRole = user.Role;
            user.Email = updateDto.Email;
            user.Role = updateDto.Role;

            if (oldRole != updateDto.Role)
            {
                if (updateDto.Role == "Admin")
                {
                    var donor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == id);
                    if (donor != null) _context.Donors.Remove(donor);

                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
                    if (admin == null)
                    {
                        _context.Admins.Add(new Admin
                        {
                            UserId = id,
                            FirstName = user.DonorProfile?.FirstName ?? "Admin",
                            LastName = user.DonorProfile?.LastName ?? "User"
                        });
                    }
                }
                else if (updateDto.Role == "Donor")
                {
                    var admin = await _context.Admins.FirstOrDefaultAsync(a => a.UserId == id);
                    if (admin != null) _context.Admins.Remove(admin);

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
            if (donor == null) throw new InvalidOperationException("Donor nije pronađen.");

            var user = await _context.Users
                .Include(u => u.AdminProfile)
                .FirstOrDefaultAsync(u => u.Id == donor.UserId);

            if (user == null)
            {
                _context.Donors.Remove(donor);
                await _context.SaveChangesAsync();
                return;
            }

            if (user.IsSuperAdmin) throw new InvalidOperationException("Ne možete obrisati SuperAdmina.");

            _context.Donors.Remove(donor);

            if (user.AdminProfile != null)
                _context.Admins.Remove(user.AdminProfile);

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.DonorProfile)
                .Include(u => u.AdminProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) throw new InvalidOperationException("User nije pronađen.");

            if (user.IsSuperAdmin) throw new InvalidOperationException("Ne možete obrisati SuperAdmina.");

            if (user.DonorProfile != null) _context.Donors.Remove(user.DonorProfile);
            if (user.AdminProfile != null) _context.Admins.Remove(user.AdminProfile);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task TransferSuperAdminAsync(int toUserId)
        {
            var currentSuperAdmin = await _context.Users.FirstOrDefaultAsync(u => u.IsSuperAdmin);
            if (currentSuperAdmin == null) throw new InvalidOperationException("Trenutni SuperAdmin nije pronađen.");

            var targetUser = await _context.Users.FindAsync(toUserId);
            if (targetUser == null) throw new InvalidOperationException("Korisnik kome zelis da das privilegiju superadmina nije pronađen.");

            if (targetUser.IsSuperAdmin) throw new InvalidOperationException("Korisnik je vec SuperAdmin.");

            if (targetUser.Role != "Admin") throw new InvalidOperationException("Samo admini mogu postati SuperAdmin.");

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
