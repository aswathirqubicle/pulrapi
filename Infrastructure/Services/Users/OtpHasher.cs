using System;
using System.Security.Cryptography;
using System.Text;
using Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Core.Infrastructure.Services.Users
{
    /// <summary>
    /// HMAC-SHA256 hasher for one-time codes. The keyed hash means an attacker who
    /// reads the database still cannot brute-force the small (6-digit) OTP keyspace
    /// offline without the server secret. The brute-force defense at runtime is the
    /// per-code attempt counter + short expiry; this protects data at rest.
    /// </summary>
    public class OtpHasher : IOtpHasher
    {
        private readonly byte[] _key;

        public OtpHasher(IConfiguration configuration)
        {
            // Dedicated secret if provided, otherwise fall back to the JWT signing key.
            var secret = configuration["Otp:HashKey"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = configuration["JWT:Key"];
            }
            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("No OTP hash key configured (set 'Otp:HashKey' or 'JWT:Key').");
            }

            _key = Encoding.UTF8.GetBytes(secret);
        }

        public string Hash(string code)
        {
            if (code == null) throw new ArgumentNullException(nameof(code));

            using var hmac = new HMACSHA256(_key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToBase64String(hash);
        }

        public bool Verify(string code, string hash)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(hash))
            {
                return false;
            }

            var computed = Hash(code);
            // Constant-time comparison to avoid timing side channels.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(hash));
        }
    }
}
