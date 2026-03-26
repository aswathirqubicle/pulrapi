using Core.Domain.Entities;
using Dashboard.Application.Mappings;
using Dashboard.Application.Mediatr.Users.Commands.Update;
using Profile = AutoMapper.Profile;

namespace Dashboard.Application.Mediatr.Users.Users 
{
    public class UserDetailsResponse : IMapFrom<User>
    {
        public string? Uid { get; set; }
        public string? FullName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public bool IsSuspended { get; set; }
        public DateTime SuspendedAt { get; set; }
        public DateTime SuspendedUntil { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<User, UserDetailsResponse>()
                 .ForMember(dest => dest.Uid, opt => opt.MapFrom(src => src.Id));
            
            profile.CreateMap<UpdateUserCommand, User>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName));
        }
    }
}