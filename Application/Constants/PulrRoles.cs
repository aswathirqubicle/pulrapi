using System.Linq;

namespace Core.Application.Constants
{
    public static class PulrRoles
    {
        public const string Administrator = "Administrator";
        public const string Moderator = "Moderator";
        public const string User = "User";
        public const string Influencer = "Influencer";
        public const string StoreOwner = "StoreOwner";
        public const string StoreManager = "StoreManager";
        public static readonly string[] Roles = { Administrator, Moderator, User, Influencer, StoreOwner, StoreManager };

        public static bool RoleExists(string role)
        {
            return Roles.Contains(role);
        }
    }
}
