
using System;
using System.Linq;

namespace Core.Application.Helpers
{
    public static class UsernameHelper
    {
        public static string Normalize(string username)
        {
            if (!string.IsNullOrWhiteSpace(username))
            {
                username = string.Concat(username.Where(c => !Char.IsWhiteSpace(c))).Replace("@", "");
            }

            return username;
        }
    }
}
