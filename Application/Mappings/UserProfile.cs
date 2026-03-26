using AutoMapperProfile = AutoMapper.Profile;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Mediatr.Users.Commands.Password;
using Core.Application.Models.Users;
using Core.Domain.Entities;

namespace Core.Application.Mappings
{
    public class UserProfile : AutoMapperProfile
    {
        public UserProfile()
        {
            CreateMap<LoginCommand, LoginDto>();
            CreateMap<User, PublicUserDto>();
            CreateMap<ChangePasswordCommand, PasswordChangeDto>();
        }
    }
}
