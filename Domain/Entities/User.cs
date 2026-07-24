using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Core.Domain.Entities
{
    public class User : IdentityUser
    {

        public async Task GetRoles(UserManager<User> userManager)
        {
            try
            {
                this.Roles = await userManager.GetRolesAsync(this);
            }
            catch (Exception e)
            {

                throw new Exception("Error getting user roles", e);
            }
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string ZipCode { get; set; }
        public int? CountryId { get; set; }
        public Country Country { get; set; }
        public string CityName { get; set; }
        public int UsernameChangesCount { get; set; }
        public DateTime UsernameChangeDate { get; set; }
        public int DisplayNameChangesCount { get; set; }
        public DateTime DisplayNameChangeDate { get; set; }
        public bool IsSuspended { get; set; }
        public bool IsVerified { get; set; }
        public bool TermsAccepted { get; set; }
        public DateTime? SuspendedAt { get; set; }
        public DateTime? SuspendedUntil { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public virtual Profile Profile { get; set; }
        public virtual Affiliate Affiliate { get; set; }
        public virtual ICollection<Store> Stores { get; set; }

        [NotMapped]
        public virtual IList<string> Roles { get; private set; }
        public virtual ICollection<Post> Posts { get; set; }
        public virtual ICollection<Story> Stories { get; set; } = new List<Story>();
        public virtual ICollection<UserBagProduct> BagItems { get; set; }
        public virtual ICollection<UserWishlistProduct> WishlistItems { get; set; }
        public virtual ICollection<ShippingDetails> ShippingDetails { get; set; }
        public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();

        // Hashed password reset code, expiry, and failed-verification counter
        public string PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiry { get; set; }
        public int PasswordResetAttempts { get; set; }

        // Hashed email verification code, expiry, and failed-verification counter
        public string EmailVerificationCode { get; set; }
        public DateTime? EmailVerificationCodeExpiry { get; set; }
        public int EmailVerificationAttempts { get; set; }
        public DateTime? LastReactivatedAt { get; set; }

        /// <summary>
        /// Stripe customer ID for payment processing.
        /// </summary>
        public string? StripeCustomerId { get; set; }

    }
}
