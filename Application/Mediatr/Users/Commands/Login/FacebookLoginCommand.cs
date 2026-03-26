using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Models.Users;

namespace Core.Application.Mediatr.Users.Commands.Login
{
    public class FacebookLoginCommand : IRequest<LoginResponse>
    {
        [Required]
        public string AccessToken { get; set; }
    }

    public class FacebookLoginCommandHandler : IRequestHandler<FacebookLoginCommand, LoginResponse>
    {
        private readonly IUserService _userService;

        public FacebookLoginCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<LoginResponse> Handle(FacebookLoginCommand request, CancellationToken cancellationToken)
        {
            return await _userService.LoginWithFacebookAsync(request.AccessToken);
        }
    }
}
