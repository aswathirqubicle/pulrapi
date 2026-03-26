using Core.Application.Models;
using Core.Domain.Entities;
using Dashboard.Application.Mappings;
using AutoMapperProfile = AutoMapper.Profile;

namespace Dashboard.Application.Mediatr.Users.Users
{
    public class UserResponse : IMapFrom<User>
    {
        public string? Id { get; set; }
        public string? FullName { get; set; } 
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsSuspended { get; set; }
        public DateTime? SuspendedUntil { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? UserName { get; set; }
        public string[]? Roles { get; set; }
        public int StoresCount { get; set; }

        public void Mapping(AutoMapperProfile profile)
        {
            profile.CreateMap<User, UserResponse>()
                .ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.Profile.ImageUrl))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles.ToList()));

            profile.CreateMap<PagedList<User>, PagingResponse<UserResponse>>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src));
        }
    }
}