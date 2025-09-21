using AutoMapper;
using SWETemplate.DTOs; // Adjust namespace based on your project structure
using SWETemplate.Models; // Adjust namespace based on your project structure

namespace SWETemplate.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings
            CreateMap<User, UserResponseDto>();
            CreateMap<CreateAdminDto, User>();
            CreateMap<UpdateUserDto, User>();
                        
            // Add reverse mappings if needed
            CreateMap<UserResponseDto, User>();
            
            // Add more mappings as needed
        }
    }
}