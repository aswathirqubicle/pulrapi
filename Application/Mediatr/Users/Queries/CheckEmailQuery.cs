using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Users;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Users.Queries
{
    public class CheckEmailQuery : IRequest<CheckEmailResponse>
    {
        public string Email { get; set; }
    }

    public class CheckEmailQueryHandler : IRequestHandler<CheckEmailQuery, CheckEmailResponse>
    {
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;

        public CheckEmailQueryHandler(UserManager<User> userManager, IApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<CheckEmailResponse> Handle(CheckEmailQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || user.IsSuspended)
            {
                return new CheckEmailResponse { Exists = false };
            }

            var profile = await _dbContext.Profiles
                .Include(p => p.ProfileOnboardingPreferences)
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            return new CheckEmailResponse 
            { 
                Exists = true,
                IsVerified = user.IsVerified,
                HasCompletedOnboarding = profile?.ProfileOnboardingPreferences?.Count > 0,
                TermsAccepted = user.TermsAccepted
            };
        }
    }
} 