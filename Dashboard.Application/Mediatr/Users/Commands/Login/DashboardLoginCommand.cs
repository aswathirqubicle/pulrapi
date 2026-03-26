using System.ComponentModel.DataAnnotations;
using Core.Application.Interfaces;
using Core.Application.Models.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Users.Commands.Login;

public class DashboardLoginCommand : IRequest<LoginResponse>
{
    [Required]
    public string? Username { get; set; }
    [Required]
    public bool IsEmail { get; set; }
    [Required]
    public string? Password { get; set; }
}

public class DashboardLoginCommandHandler : IRequestHandler<DashboardLoginCommand,LoginResponse>
{
    private readonly IUserService _userService;
    private readonly ILogger<DashboardLoginCommandHandler> _logger;

    public DashboardLoginCommandHandler(
        IUserService userService,
        ILogger<DashboardLoginCommandHandler> logger
        )
    {
        _userService = userService;
        _logger = logger;
    }
    
    public async Task<LoginResponse> Handle(DashboardLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {

            var x = await _userService.LoginAsync(new LoginDto
            {
                Username = request.Username,
                IsEmail = request.IsEmail,
                Password = request.Password
            });
            
            return x;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error logging in");
            throw;
        }
    }
}