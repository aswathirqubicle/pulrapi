using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using System.Collections.Generic;

namespace Core.Infrastructure.Services.Users
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly ILogger<CurrentUserService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<User> _userManager;
        private readonly IApplicationDbContext _dbContext;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CurrentUserService(
            ILogger<CurrentUserService> logger,
            IHttpContextAccessor httpContextAccessor,
            UserManager<User> userManager,
            IApplicationDbContext dbContext,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _dbContext = dbContext;
            _roleManager = roleManager;
        }

        public async Task<List<IdentityRole>> GetRolesAsync()
        {
            return await _roleManager.Roles.ToListAsync();
        }

        public bool IsUserLoggedIn()
        {
            if (_httpContextAccessor.HttpContext?.User == null)
            {
                return false;
            }
            
            return _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated;
        }

        public string GetUserId()
        {
            return _userManager.GetUserId(_httpContextAccessor.HttpContext.User);
        }

        public bool HasRole(string role)
        {
            if (_httpContextAccessor.HttpContext?.User == null)
            {
                return false;
            }

            return _httpContextAccessor.HttpContext.User.IsInRole(role);
        }

        public bool HasAnyOfRoles(string[] userRoles)
        {
            if (_httpContextAccessor.HttpContext?.User == null)
            {
                return false;
            }

            foreach (string role in userRoles)
            {
                if (_httpContextAccessor.HttpContext.User.IsInRole(role))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<User> GetUserAsync(bool skipDetails = false, bool includeStores = false)
        {
            try
            {
                User user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext?.User);
                if(user == null) return null;

                await user.GetRoles(_userManager);
                if (skipDetails)
                {
                    return user;
                }
                user.Profile = await _dbContext.Profiles
                    .Include(p => p.Currency)
                    .Include(p => p.ProfileSocialMedia)
                    .Include(p => p.ProfileSocialMediaLinks)
                    .Include(p => p.Gender)
                    .Include(p => p.User)
                    .Include(p => p.User.Country)
                    .SingleOrDefaultAsync(p => p.UserId == user.Id);
                user.Country = await _dbContext.Countries.SingleOrDefaultAsync(c => c.Id == user.CountryId);
                if(includeStores)
                {
                    user.Stores = await _dbContext.Stores.Where(s => s.UserId == user.Id).ToListAsync();
                }
                return user;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public string GetToken()
        {
           return _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", "");
        }
    }
}
