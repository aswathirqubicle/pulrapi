using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Core.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string GetUserId();
        string GetToken();
        Task<User> GetUserAsync(bool skipDetails = false, bool includeStores = false);
        bool IsUserLoggedIn();
        bool HasRole(string userRole);
        bool HasAnyOfRoles(string[] userRoles);
        Task<List<IdentityRole>> GetRolesAsync();
    }
}
