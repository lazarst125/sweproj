using System.Collections.Generic;
using System.Threading.Tasks;
using SWETemplate.DTOs;


namespace SWETemplate.Services
{
public interface IAdminService
{
Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
Task PromoteToAdminAsync(int userId);
Task DemoteFromAdminAsync(int userId);
Task<UserResponseDto> UpdateUserAsync(int id, UpdateUserDto updateDto);
Task DeleteDonorAsync(int donorId);
Task DeleteUserAsync(int userId);
Task TransferSuperAdminAsync(int toUserId);
}
}