using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Constants.Http;
using Core.Application.Exceptions;
using Core.Application.Extensions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Currencies;
using Core.Application.Models.External.Apple;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Application.Models.Users;
using Core.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using shortid;
using shortid.Configuration;

namespace Core.Infrastructure.Services.Users
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IEmailLogoService _emailLogoService;
        private readonly IFacebookAuthService _facebookAuthService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IProfileService _profileService;
        private readonly IMapper _mapper;
        private readonly JWT _jwt;
        private readonly IAppleAuthService _appleAuthService;
        private readonly INotificationService _notificationService;
        private readonly SignInManager<User> _signInManager;
        private readonly IOtpHasher _otpHasher;

        // After this many failed OTP verifications the code is invalidated and a new one must be requested.
        private const int MaxOtpAttempts = 5;

        public UserService(
            ILogger<UserService> logger,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<JWT> jwt,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IConfiguration configuration,
            IEmailService emailService,
            IEmailLogoService emailLogoService,
            IFacebookAuthService facebookAuthService,
            IGoogleAuthService googleAuthService,
            IProfileService profileService,
            IMapper mapper,
            IAppleAuthService appleAuthService,
            INotificationService notificationService,
            SignInManager<User> signInManager,
            IOtpHasher otpHasher)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _emailLogoService = emailLogoService ?? throw new ArgumentNullException(nameof(emailLogoService));
            _facebookAuthService = facebookAuthService ?? throw new ArgumentNullException(nameof(facebookAuthService));
            _googleAuthService = googleAuthService ?? throw new ArgumentNullException(nameof(googleAuthService));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _jwt = jwt?.Value ?? throw new ArgumentNullException(nameof(jwt));
            _appleAuthService = appleAuthService ?? throw new ArgumentNullException(nameof(appleAuthService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _otpHasher = otpHasher ?? throw new ArgumentNullException(nameof(otpHasher));
        }

        public async Task<string> GetRoleIdAsync(string roleName)
        {
            var role = await _dbContext.Set<IdentityRole>()
                .SingleOrDefaultAsync(r => r.Name == roleName);

            return role.Id;
        }

        public IQueryable<IdentityUserRole<string>> IsUserInRoleQuery(string userId, string roleId)
        {
            var userRoleQuery = _dbContext.Set<IdentityUserRole<string>>()
                .Where(ur => ur.UserId == userId && ur.RoleId == roleId);

            return userRoleQuery;
        }

        public async Task<UserRegisterResponseDto> RegisterAsync(UserRegisterDto model)
        {
            bool isSuccess = false;
            string message = "";

            User user = null;
            var userWithSameEmail = await _userManager.FindByEmailAsync(model.Email);
            if (userWithSameEmail != null)
            {
                if (userWithSameEmail.IsSuspended)
                {
                    // Check if the suspension period has expired
                    if (userWithSameEmail.SuspendedUntil.HasValue && userWithSameEmail.SuspendedUntil.Value > DateTime.UtcNow)
                    {
                        // User is still within the 30-day period, reactivate their account
                        await ReactivateUserAsync(userWithSameEmail);
                        message = "Your account has been reactivated. You can now log in.";
                        return new UserRegisterResponseDto()
                        {
                            IsSuccess = true,
                            Message = message,
                            User = userWithSameEmail,
                            IsNewUser = false
                        };
                    }
                    else
                    {
                        // Suspension period has expired, allow new registration
                        // Update the suspended user's email to free up the email address
                        var timestamp = DateTime.UtcNow.Ticks;
                        userWithSameEmail.Email = $"suspended_{timestamp}_{userWithSameEmail.Email}";
                        userWithSameEmail.UserName = $"suspended_{timestamp}_{userWithSameEmail.UserName}";
                        await _userManager.UpdateAsync(userWithSameEmail);

                        // Create a new user with the original email
                        user = new User
                        {
                            FirstName = model.FirstName,
                            LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName,
                            DisplayName = !string.IsNullOrEmpty(model.DisplayName) ? model.DisplayName : await GenerateUniqueDisplayName(model.FirstName, model.LastName),
                            UserName = UsernameHelper.Normalize(model.Username),
                            Email = model.Email,
                            PhoneNumber = model.PhoneNumber,
                            TermsAccepted = model.TermsAccepted,
                            DateOfBirth = model.DateOfBirth,
                            CreatedAt = DateTime.UtcNow,
                            IsSuspended = false,
                            IsVerified = true, // Set to true since email is already verified
                        };
                        var result = await _userManager.CreateAsync(user, model.Password);
                        if (result.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, PulrRoles.User);
                            isSuccess = true;
                        }
                        else
                        {
                            message = string.Join(", ", result.Errors.Select(e => e.Description));
                        }
                    }
                }
                else if (userWithSameEmail.EmailConfirmed && !userWithSameEmail.IsVerified)
                {
                    // Update the existing user's details and mark as verified
                    userWithSameEmail.FirstName = model.FirstName;
                    userWithSameEmail.LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName;
                    userWithSameEmail.DisplayName = !string.IsNullOrEmpty(model.DisplayName)
                        ? model.DisplayName
                        : await GenerateUniqueDisplayName(model.FirstName, model.LastName);
                    userWithSameEmail.UserName = UsernameHelper.Normalize(model.Username);
                    userWithSameEmail.PhoneNumber = model.PhoneNumber;
                    userWithSameEmail.TermsAccepted = model.TermsAccepted;
                    userWithSameEmail.DateOfBirth = model.DateOfBirth;
                    userWithSameEmail.IsVerified = true;
                    userWithSameEmail.EmailConfirmed = true;
                    userWithSameEmail.IsSuspended = false;
                    userWithSameEmail.UpdatedAt = DateTime.UtcNow;
                    // ... update any other fields as needed

                    await _userManager.UpdateAsync(userWithSameEmail);
                    await _userManager.AddToRoleAsync(userWithSameEmail, PulrRoles.User);

                    // If a password is provided (normal registration), set it
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(userWithSameEmail);
                        await _userManager.ResetPasswordAsync(userWithSameEmail, token, model.Password);
                    }

                    isSuccess = true;
                    user = userWithSameEmail;
                    message = "User profile completed successfully.";
                }
                else if (userWithSameEmail.IsVerified)
                {
                    message = "User already Registered with this Email";
                }
                else
                {
                    message = HttpErrorMessages.EmailTaken;
                }
            }
            else
            {
                // New user registration
                user = new User
                {
                    FirstName = model.FirstName,
                    LastName = string.IsNullOrWhiteSpace(model.LastName) ? null : model.LastName,
                    DisplayName = !string.IsNullOrEmpty(model.DisplayName) ? model.DisplayName : await GenerateUniqueDisplayName(model.FirstName, model.LastName),
                    UserName = UsernameHelper.Normalize(model.Username),
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    TermsAccepted = model.TermsAccepted,
                    DateOfBirth = model.DateOfBirth,
                    CreatedAt = DateTime.UtcNow,
                    IsSuspended = false,
                    IsVerified = true,
                };
                IdentityResult result;
                if (!string.IsNullOrEmpty(model.Password))
                {
                    result = await _userManager.CreateAsync(user, model.Password);
                }
                else
                {
                    result = await _userManager.CreateAsync(user);
                }
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, PulrRoles.User);
                    isSuccess = true;
                }
                else
                {
                    message = string.Join(", ", result.Errors.Select(e => e.Description));
                }
            }

            if (isSuccess)
            {
                if (model.CountryUid != null)
                {
                    var country = await _dbContext.Countries.Where(c => c.Uid == model.CountryUid).FirstOrDefaultAsync();
                    if (country != null)
                    {
                        user.Country = country;
                    }
                }

                var affiliateId = ShortId.Generate(new GenerationOptions(true, false));
                user.Affiliate = new Affiliate() { AffiliateId = affiliateId };

                // Save CommunicationMail in SellerSettings if provided
                if (!string.IsNullOrWhiteSpace(model.CommunicationMail))
                {
                    var sellerSettings = await _dbContext.SellerSettings
                        .FirstOrDefaultAsync(s => s.UserId == user.Id);

                    if (sellerSettings == null)
                    {
                        sellerSettings = new SellerSettings
                        {
                            UserId = user.Id,
                            CommunicationMail = model.CommunicationMail
                        };
                        _dbContext.SellerSettings.Add(sellerSettings);
                    }
                    else
                    {
                        sellerSettings.CommunicationMail = model.CommunicationMail;
                    }
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }

            return new UserRegisterResponseDto()
            {
                IsSuccess = isSuccess,
                Message = message,
                User = isSuccess ? user : null,
                IsNewUser = userWithSameEmail == null, // true if user was just created
                HasCompletedOnboarding = false // New users haven't completed onboarding yet
            };
        }

        public async Task SendEmailConfirmationToken(User user)
        {
            try
            {
                // var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                // var confirmationLink =
                //     $"{_configuration["ApiUrl"]}/users/confirm-email?email={user.Email}&token={token}";
                var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                user.EmailVerificationCode = _otpHasher.Hash(code); // store hashed, never plaintext
                user.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15); // 15 min expiry
                user.EmailVerificationAttempts = 0;
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                var emailContent = $@"
<div style=""font-family: Arial, sans-serif; text-align: center; background: #fff; padding: 32px;"">
  <img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR Logo"" width=""80"" height=""27"" style=""width: 80px; height: 27px; margin-bottom: 24px; display: block; border: 0;"" />
  <h2>Verify your email address</h2>
  <p>Use the following code to verify your email address:</p>
  <div style=""font-size: 2em; font-weight: bold; letter-spacing: 8px; margin: 24px 0;"">{code}</div>
  <p>This code will expire in 15 minutes.</p>
  <p>If you didn't create an account, you can ignore this email.</p>
</div>
";
                var emailParams = new EmailParamsDto()
                {
                    From = _configuration["PulrEmails:Support"],
                    Subject = "Confirmation link for Pulr.co to verify your email address",
                    Content = emailContent,
                    To = new List<string>() { user.Email },
                    IsTemplateFromFile = false,
                };

                // Add logo attachment using service (follows Dependency Inversion Principle)
                await emailParams.AddLogoAsync(_emailLogoService);

                await _emailService.SendMail(emailParams, includeAttachments: emailParams.Attachments.Count > 0);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<AuthModel> GetTokenAsync(TokenRequest request)
        {
            var authModel = new AuthModel();
            User user;

            if (request.IsEmail)
            {
                user = await _userManager.FindByEmailAsync(request.Username);              
            }
            else
            {
                var normalizedUsername = UsernameHelper.Normalize(request.Username);
                user = await _userManager.FindByNameAsync(normalizedUsername);
            }

            if (user == null)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = HttpErrorMessages.WrongCredentials;
                return authModel;
            }

            if (user.IsSuspended)
            {
                // Check if the suspension period has expired
                if (user.SuspendedUntil.HasValue && user.SuspendedUntil.Value > DateTime.UtcNow)
                {
                    // User is still within the 30-day period, reactivate their account
                    await ReactivateUserAsync(user);    
                }
                else
                {
                    // Suspension period has expired, keep the account suspended
                    authModel.IsAuthenticated = false;
                    authModel.Message = HttpErrorMessages.AccountSuspended;
                    return authModel;
                }
            }

            // After possible reactivation, check if user's profile is active
            var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
            
            // For OAuth users: only skip profile check if no profile exists (new users)
            // If profile exists, we need to check its status even for OAuth users
            if (profile == null)
            {
                // Email login with no profile - reject
                authModel.IsAuthenticated = false;
                authModel.Message = "User account is deactivated";
                return authModel;
            }
            else
            {
                // Profile exists - check its status
                if (!profile.IsActive && !user.IsSuspended)
                {
                    await ReactivateUserAsync(user);
                    profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
                }

                if (!profile.IsActive)
                {
                    authModel.IsAuthenticated = false;
                    authModel.Message = "User account is deactivated";
                    return authModel;
                }
            }

            // Lockout-aware password check: increments AccessFailedCount on failure,
            // locks the account at the configured threshold, and resets the counter
            // on success. Does not create an auth cookie.
            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (signInResult.IsLockedOut)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = "Account temporarily locked due to too many failed attempts. Please try again later.";
                return authModel;
            }

            if (signInResult.Succeeded)
            {
                authModel = await CreateSuccessAuthModel(user);
                return authModel;
            }

            authModel.IsAuthenticated = false;
            authModel.Message = HttpErrorMessages.WrongCredentials;
            return authModel;
        }

        private async Task<AuthModel> CreateSuccessAuthModel(User user)
        {
            try
            {
                var authModel = new AuthModel();
                authModel.IsAuthenticated = true;
                authModel.UserId = user.Id;
                JwtSecurityToken jwtSecurityToken = await CreateJwtToken(user);
                authModel.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
                authModel.Email = user.Email;
                authModel.Username = user.UserName;
                var rolesList = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
                authModel.Roles = rolesList.ToList();
                return authModel;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<JwtSecurityToken> CreateJwtToken(User user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = new List<Claim>();
            for (int i = 0; i < roles.Count; i++)
            {
                roleClaims.Add(new Claim("roles", roles[i]));
            }

            var now = DateTime.UtcNow;
            var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new Claim(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                }
                .Union(userClaims)
                .Union(roleClaims);
            // Convert hex string to byte array if the key is in hex format
            byte[] keyBytes;
            if (string.IsNullOrEmpty(_jwt.Key))
            {
                throw new InvalidOperationException("JWT:Key cannot be null or empty.");
            }
            
            if (_jwt.Key.All(c => "0123456789ABCDEFabcdef".Contains(c)) && _jwt.Key.Length % 2 == 0)
            {
                // Key is in hex format - use manual conversion for compatibility
                keyBytes = new byte[_jwt.Key.Length / 2];
                for (int i = 0; i < keyBytes.Length; i++)
                {
                    keyBytes[i] = Convert.ToByte(_jwt.Key.Substring(i * 2, 2), 16);
                }
            }
            else
            {
                // Key is in UTF-8 format
                keyBytes = Encoding.UTF8.GetBytes(_jwt.Key);
            }
            
            // Validate key length for HS256 (minimum 32 bytes = 256 bits)
            if (keyBytes.Length < 32)
            {
                throw new InvalidOperationException($"JWT:Key is too short. Minimum 32 bytes required, got {keyBytes.Length} bytes.");
            }
            var symmetricSecurityKey = new SymmetricSecurityKey(keyBytes);
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.DurationInMinutes),
                signingCredentials: signingCredentials);
            return jwtSecurityToken;
        }

        public async Task DeleteAccountAsync(User currentUser)
        {
            try
            {
                // Suspend the user
                currentUser.IsSuspended = true;
                currentUser.SuspendedAt = DateTime.UtcNow;
                currentUser.SuspendedUntil = DateTime.UtcNow.AddDays(30);
                //currentUser.SuspendedUntil = DateTime.UtcNow.AddMinutes(2);
                await _userManager.UpdateAsync(currentUser);

                // Deactivate the user's profile
                var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == currentUser.Id);
                if (profile != null)
                {
                    profile.IsActive = false;
                }

                // Suspend all user's stores
                var stores = await _dbContext.Stores.Where(s => s.UserId == currentUser.Id).ToListAsync();
                foreach (var store in stores)
                {
                    store.IsActive = false;
                }

                // Suspend all user's posts
                var posts = await _dbContext.Posts.Where(p => p.User.Id == currentUser.Id).ToListAsync();
                foreach (var post in posts)
                {
                    post.IsActive = false;
                }

                // Suspend all user's comments
                var comments = await _dbContext.Comments.Where(c => c.CommentedBy.UserId == currentUser.Id).ToListAsync();
                foreach (var comment in comments)
                {
                    comment.IsActive = false;
                }

                // Suspend all user's stories
                var stories = await _dbContext.Stories.Where(s => s.UserId == currentUser.Id).ToListAsync();
                foreach (var story in stories)
                {
                    story.IsActive = false;
                }

                // suspend all user's products
                var products = await _dbContext.Products.Where(p => p.UserId == currentUser.Id).ToListAsync();
                foreach (var product in products)
                {
                    product.IsActive = false;
                }

                // Soft-delete all user's shipping addresses
                var shippingDetails = await _dbContext.ShippingDetails.Where(sd => sd.UserId == currentUser.Id).ToListAsync();
                foreach (var sd in shippingDetails)
                {
                    sd.IsActive = false;
                    sd.UserId = null; // Unlink to satisfy soft-delete requirements for order history
                }

                // Clean up all push tokens for the suspended user
                try
                {
                    await CleanupAllPushTokensForUserAsync(currentUser.Id);
                    _logger.LogInformation("Cleaned up all push tokens for suspended user {UserId}", currentUser.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up push tokens for suspended user {UserId}", currentUser.Id);
                }

                //profile.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }


        public async Task ReactivateUserAsync(User user)
        {
            try
            {
                // Reactivate the user
                user.IsSuspended = false;
                user.SuspendedAt = null;
                user.SuspendedUntil = null;
                user.LastReactivatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Reactivate the user's profile
                var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
                if (profile != null)
                {
                    profile.IsActive = true;
                }

                // Reactivate all user's stores
                var stores = await _dbContext.Stores.Where(s => s.UserId == user.Id).ToListAsync();
                foreach (var store in stores)
                {
                    store.IsActive = true;
                }

                // Reactivate all user's posts
                var posts = await _dbContext.Posts.Where(p => p.User.Id == user.Id).ToListAsync();
                foreach (var post in posts)
                {
                    post.IsActive = true;
                }

                // Reactivate all user's comments
                var comments = await _dbContext.Comments.Where(c => c.CommentedBy.UserId == user.Id).ToListAsync();
                foreach (var comment in comments)
                {
                    comment.IsActive = true;
                }

                // Reactivate all user's stories
                var stories = await _dbContext.Stories.Where(s => s.UserId == user.Id).ToListAsync();
                foreach (var story in stories)
                {
                    story.IsActive = true;
                }

                // Reactivate all user's products
                var products = await _dbContext.Products.Where(p => p.UserId == user.Id).ToListAsync();
                foreach (var product in products)
                {
                    product.IsActive = true;
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task ManagePasswordResetRequest(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    throw new NotFoundException("User not found.");
                }
                // var userId = await _userManager.GetUserIdAsync(user);
                // var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                // code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                // var passwordResetUrl =
                // $"{_configuration["ConsumerUrls:WebApp"]}/password-reset?email={email}&token={code}";
                var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                user.PasswordResetCode = _otpHasher.Hash(code); // store hashed, never plaintext
                user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15); // 15 min expiry
                user.PasswordResetAttempts = 0;
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                var emailContent = $@"
<div style=""font-family: Arial, sans-serif; text-align: center; background: #fff; padding: 32px;"">
  <img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR Logo"" width=""80"" height=""27"" style=""width: 80px; height: 27px; margin-bottom: 24px; display: block; border: 0;"" />
  <h2>Reset your password</h2>
  <p>Use the following code to reset your password:</p>
  <div style=""font-size: 2em; font-weight: bold; letter-spacing: 8px; margin: 24px 0;"">{code}</div>
  <p>If you didn't request a password reset, you can ignore this email.</p>
</div>
";
                var emailParams = new EmailParamsDto()
                {
                    From = _configuration["PulrEmails:Support"],
                    Subject = "Password reset code",
                    Content = emailContent,
                    To = new List<string>() { email },
                    IsTemplateFromFile = false,
                };

                // Add logo attachment using service (follows Dependency Inversion Principle)
                await emailParams.AddLogoAsync(_emailLogoService);

                await _emailService.SendMail(emailParams, includeAttachments: emailParams.Attachments.Count > 0);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task AssignRole(string storeOwner)
        {
            try
            {
                if (PulrRoles.RoleExists(storeOwner) == false)
                {
                    throw new ForbiddenException($"Role {storeOwner} doesn't exist.");
                }

                var user = await _userManager.FindByIdAsync(_currentUserService.GetUserId());

                if (await _userManager.IsInRoleAsync(user, storeOwner))
                {
                    return;
                }

                var res = await _userManager.AddToRoleAsync(user, storeOwner);

                if (!res.Succeeded)
                {
                    throw new Exception(string.Join(",", res.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<LoginResponse> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var normalizedUsername = loginDto.IsEmail ? loginDto.Username : UsernameHelper.Normalize(loginDto.Username);
                var res = await GetTokenAsync(new TokenRequest()
                {
                    IsEmail = loginDto.IsEmail,
                    Username = normalizedUsername,
                    Password = loginDto.Password
                });

                if (!res.IsAuthenticated)
                {
                    _logger.LogWarning($"Login failed for user {normalizedUsername} - {res.Message}");
                    throw new NotAuthenticatedException(res.Message);
                }

                var user = await _userManager.FindByIdAsync(res.UserId);
                bool wasReactivated = false;
                if (user != null && user.LastReactivatedAt.HasValue && user.LastReactivatedAt.Value > DateTime.UtcNow.AddDays(-30))
                {
                    wasReactivated = true;
                    user.LastReactivatedAt = null;
                    await _userManager.UpdateAsync(user);
                }

                var loginResponse = new LoginResponse()
                {
                    Id = res.UserId,
                    Roles = res.Roles,
                    Token = res.Token,
                    Username = res.Username,
                    Email = res.Email,
                    ImageUrl = null,
                    ShowWelcomeBack = wasReactivated,
                };
                
                var profile = await _dbContext.Profiles
                    .Where(p => p.UserId == res.UserId)
                    .Select(p => new LoginResponse
                    {
                        ProfileUid = p.Uid,
                        FullName = p.User.FirstName,
                        FirstName = p.User.FirstName,
                        LastName = p.User.LastName,
                        Username = p.User.UserName,
                        ImageUrl = p.ImageUrl,
                        Currency = _mapper.Map<CurrencyDetailsResponse>(p.Currency),
                        StoreUids = p.User.Stores.Select(s => s.Uid).ToList()
                    }).SingleOrDefaultAsync();


                if (profile != null)
                {
                    loginResponse.ProfileUid = profile.ProfileUid;
                    loginResponse.ImageUrl = profile.ImageUrl;
                    loginResponse.StoreUids = profile.StoreUids;
                    loginResponse.FullName = profile.FirstName;
                    loginResponse.FirstName = profile.FirstName;
                    loginResponse.LastName = profile.LastName;
                    loginResponse.PhoneNumber = profile.PhoneNumber;
                    loginResponse.Currency = profile.Currency;
                }

                //get the profileid with the userId
                var userProfile = await _dbContext.Profiles
                    .Where(p => p.UserId == res.UserId)
                    .Select(p => new { p.Id })
                    .SingleOrDefaultAsync();

                // Set unread notification count
                var unreadCount = await _dbContext.NotificationHistories
                    .Where(n => n.ReceiverUserId == userProfile.Id && !n.IsRead)
                    .CountAsync();
                loginResponse.UnreadNotificationCount = unreadCount;
                loginResponse.PhoneNumber = user.PhoneNumber;

                // Check onboarding completion status
                var onboardingPreferencesCount = await _dbContext.ProfileOnboardingPreferences
                    .Where(p => p.ProfileId == userProfile.Id)
                    .CountAsync();
                loginResponse.HasCompletedOnboarding = onboardingPreferencesCount > 0;

                // Generate and store refresh token (store hash; send raw token to client)
                var refreshToken = GenerateSecureToken();
                var refreshTokenEntity = new RefreshToken
                {
                    UserId = res.UserId,
                    Token = HashRefreshToken(refreshToken),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    DeviceIdentifier = loginDto.Device.DeviceIdentifier
                };
                _dbContext.RefreshTokens.Add(refreshTokenEntity);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                loginResponse.RefreshToken = refreshToken;

                // Save login activity with app version
                await SaveLoginActivityAsync(res.UserId, loginDto.Device.Brand, loginDto.Device.ModelName, loginDto.Device.OsVersion, loginDto.Device.DeviceIdentifier, loginDto.Device.AppVersion, "Logged in");

                var (bagItemsCount, wishlistItemsCount, bagItemsTotalQuantity) = await GetUserCollectionCountsAsync(res.UserId);
                loginResponse.BagItemsCount = bagItemsCount;
                loginResponse.WishlistItemsCount = wishlistItemsCount;
                loginResponse.BagItemsTotalQuantity = bagItemsTotalQuantity;

                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private string GenerateSecureToken()
        {
            var randomNumber = new byte[64];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private static string HashRefreshToken(string token)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private async Task<AuthModel> GetAuthModelForExternalLogin(User user)
        {
            var authModel = new AuthModel();

            if (user.IsSuspended)
            {
                if (user.SuspendedUntil.HasValue && user.SuspendedUntil.Value > DateTime.UtcNow)
                {
                    await ReactivateUserAsync(user);
                }
                else
                {
                    authModel.IsAuthenticated = false;
                    authModel.Message = HttpErrorMessages.AccountSuspended;
                    return authModel;
                }
            }

            var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);

            if (profile != null)
            {
                if (!profile.IsActive && !user.IsSuspended)
                {
                    await ReactivateUserAsync(user);
                    profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
                }

                if (!profile.IsActive)
                {
                    authModel.IsAuthenticated = false;
                    authModel.Message = "User account is deactivated";
                    return authModel;
                }
            }

            return await CreateSuccessAuthModel(user);
        }

        private async Task<(int bagItemsCount, int wishlistItemsCount, int bagItemsTotalQuantity)> GetUserCollectionCountsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return (0, 0, 0);
            }

            var bagItemsCount = await _dbContext.UserBagProducts
                .CountAsync(bp => bp.UserId == userId, cancellationToken);
            var wishlistItemsCount = await _dbContext.UserWishlistProducts
                .CountAsync(wp => wp.UserId == userId, cancellationToken);
            var bagItemsTotalQuantity = await _dbContext.UserBagProducts
                .Where(bp => bp.UserId == userId)
                .SumAsync(bp => (int?)bp.Quantity ?? 0, cancellationToken);

            return (bagItemsCount, wishlistItemsCount, bagItemsTotalQuantity);
        }

        public async Task<string> GenerateUniqueUsername()
        {
            try
            {
                string generatedPart = ShortId.Generate(new GenerationOptions(true, false, 8));

                bool usernameExists = true;

                while (usernameExists)
                {
                    var uniqueUsername = "user_" + generatedPart;
                    usernameExists = await _dbContext.Users.AnyAsync(u => u.UserName == uniqueUsername);
                    if (!usernameExists)
                    {
                        return uniqueUsername;
                    }

                    generatedPart = ShortId.Generate(new GenerationOptions(true, false, 8));
                }

                throw new Exception("GenerateUniqueUsername failed");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<LoginResponse> LoginWithFacebookAsync(string accessToken)
        {
            try
            {
                var validatedTokenResult = await _facebookAuthService.ValidateAccessTokenAsync(accessToken);

                if (!validatedTokenResult.Data.IsValid)
                {
                    throw new NotAuthenticatedException("Invalid facebook token.");
                }

                var userInfo = await _facebookAuthService.GetUserInfoAsync(accessToken);
                var user = await _userManager.FindByEmailAsync(userInfo.Email);

                if (user != null && user.IsSuspended)
                {
                    throw new NotAuthenticatedException("This account has been suspended");
                }

                AuthModel authResult = null;
                bool wasReactivated = false;

                if (user == null)
                {
                    var newUser = new User()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = await GenerateUniqueUsername(),
                        Email = userInfo.Email,
                        FirstName = userInfo.FirstName,
                        LastName = userInfo.LastName,
                    };
                    var userCreateResult = await _userManager.CreateAsync(newUser);

                    if (!userCreateResult.Succeeded)
                    {
                        throw new NotAuthenticatedException("Failed to create user from facebook.");
                    }

                    var addToRoleRes = await _userManager.AddToRoleAsync(newUser, PulrRoles.User);
                    if (!addToRoleRes.Succeeded)
                    {
                        throw new NotAuthenticatedException("Failed to create user ROLE from facebook user.");
                    }

                    await _profileService.Create(newUser);

                    authResult = await CreateSuccessAuthModel(newUser);
                }
                else
                {
                    // After authentication, check if user was just reactivated
                    wasReactivated = false;
                    if (user != null && user.LastReactivatedAt.HasValue && user.LastReactivatedAt.Value > DateTime.UtcNow.AddDays(-30))
                    {
                        wasReactivated = true;
                        user.LastReactivatedAt = null;
                        await _userManager.UpdateAsync(user);
                    }
                    authResult = await CreateSuccessAuthModel(user);
                }

                _logger.LogInformation($"User '{authResult.Username}' logged in");

                var loginResponse = new LoginResponse()
                {
                    Id = authResult.UserId,
                    Roles = authResult.Roles,
                    Token = authResult.Token,
                    Username = authResult.Username,
                    Email = authResult.Email,
                    ImageUrl = null,
                    ShowWelcomeBack = wasReactivated
                };

                var profile = await _dbContext.Profiles
                                   .Where(p => p.UserId == user.Id)
                                   .Select(p => new LoginResponse
                                   {
                                       ProfileUid = p.Uid,
                                       FullName = p.User.FirstName,
                                       FirstName = p.User.FirstName,
                                       LastName = p.User.LastName,
                                       Username = p.User.UserName,
                                       ImageUrl = p.ImageUrl,
                                       Currency = _mapper.Map<CurrencyDetailsResponse>(p.Currency),
                                       StoreUids = p.User.Stores.Select(s => s.Uid).ToList()
                                   }).SingleOrDefaultAsync();


                if (profile != null)
                {
                    loginResponse.ProfileUid = profile.ProfileUid;
                    loginResponse.ImageUrl = profile.ImageUrl;
                    loginResponse.StoreUids = profile.StoreUids;
                    loginResponse.FullName = profile.FirstName;
                    loginResponse.FirstName = profile.FirstName;
                    loginResponse.LastName = profile.LastName;
                    loginResponse.PhoneNumber = profile.PhoneNumber;
                }

                // Check onboarding completion status
                if (profile != null)
                {
                    var userProfile = await _dbContext.Profiles
                        .Where(p => p.UserId == user.Id)
                        .Select(p => new { p.Id })
                        .SingleOrDefaultAsync();
                    
                    if (userProfile != null)
                    {
                        var onboardingPreferencesCount = await _dbContext.ProfileOnboardingPreferences
                            .Where(p => p.ProfileId == userProfile.Id)
                            .CountAsync();
                        loginResponse.HasCompletedOnboarding = onboardingPreferencesCount > 0;
                    }
                }

                var (bagItemsCount, wishlistItemsCount, bagItemsTotalQuantity) = await GetUserCollectionCountsAsync(loginResponse.Id);
                loginResponse.BagItemsCount = bagItemsCount;
                loginResponse.WishlistItemsCount = wishlistItemsCount;
                loginResponse.BagItemsTotalQuantity = bagItemsTotalQuantity;

                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<LoginResponse> LoginWithGoogleAsync(string accessToken, string firstName = null, string lastName = null, string pictureUrl = null, bool isEmailVerified = false, DeviceDto device = null)
        {
            try
            {
                var userInfo = await _googleAuthService.GetUserInfoAsync(accessToken);
                var user = await _userManager.FindByEmailAsync(userInfo.Email);

                AuthModel authResult = null;
                bool wasReactivated = false;

                if (user == null)
                {
                    // Create new user
                    var newUser = new User()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = await GenerateUniqueUsername(),
                        Email = userInfo.Email,
                        FirstName = userInfo.Given_Name,
                        LastName = userInfo.Family_Name,
                        DisplayName = GenerateUniqueDisplayName(userInfo.Given_Name, userInfo.Family_Name).Result,
                        EmailConfirmed = userInfo.EmailVerified,
                        CreatedAt = DateTime.UtcNow,
                        IsSuspended = false
                    };
                    var userCreateResult = await _userManager.CreateAsync(newUser);

                    if (!userCreateResult.Succeeded)
                    {
                        var errors = string.Join(", ", userCreateResult.Errors.Select(e => e.Description));
                        throw new NotAuthenticatedException($"Failed to create user from Google: {errors}");
                    }

                    var addToRoleRes = await _userManager.AddToRoleAsync(newUser, PulrRoles.User);
                    if (!addToRoleRes.Succeeded)
                    {
                        throw new NotAuthenticatedException("Failed to create user ROLE from Google user.");
                    }

                    //await _profileService.Create(newUser);

                    // Set initial profile image from Google for new users
                    if (!string.IsNullOrEmpty(pictureUrl) || !string.IsNullOrEmpty(userInfo.Picture))
                    {
                        var userProfile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == newUser.Id);
                        if (userProfile != null)
                        {
                            // For new users, it's safe to set the Google image as initial profile image
                            userProfile.ImageUrl = !string.IsNullOrEmpty(pictureUrl) ? pictureUrl : userInfo.Picture;
                            await _dbContext.SaveChangesAsync(CancellationToken.None);
                        }
                    }

                    authResult = await CreateSuccessAuthModel(newUser);
                    user = newUser;
                }
                else
                {
                    var tokenResult = await GetAuthModelForExternalLogin(user);

                    if (!tokenResult.IsAuthenticated)
                    {
                        // If account is suspended beyond 30 days, create new user
                        if (tokenResult.Message == HttpErrorMessages.AccountSuspended)
                        {
                            // Update the suspended user's email to free up the email address
                            var timestamp = DateTime.UtcNow.Ticks;
                            user.Email = $"suspended_{timestamp}_{user.Email}";
                            user.UserName = $"suspended_{timestamp}_{user.UserName}";
                            await _userManager.UpdateAsync(user);

                            var newUser = new User()
                            {
                                Id = Guid.NewGuid().ToString(),
                                UserName = await GenerateUniqueUsername(),
                                Email = userInfo.Email,
                                FirstName = userInfo.Given_Name,
                                LastName = userInfo.Family_Name,
                                DisplayName = GenerateUniqueDisplayName(userInfo.Given_Name, userInfo.Family_Name).Result,
                                EmailConfirmed = userInfo.EmailVerified,
                                CreatedAt = DateTime.UtcNow,
                                IsSuspended = false
                            };
                            var userCreateResult = await _userManager.CreateAsync(newUser);

                            if (!userCreateResult.Succeeded)
                            {
                                var errors = string.Join(", ", userCreateResult.Errors.Select(e => e.Description));
                                throw new NotAuthenticatedException($"Failed to create user from Google: {errors}");
                            }

                            var addToRoleRes = await _userManager.AddToRoleAsync(newUser, PulrRoles.User);
                            if (!addToRoleRes.Succeeded)
                            {
                                throw new NotAuthenticatedException("Failed to create user ROLE from Google user.");
                            }

                            // Set initial profile image from Google for new users
                            if (!string.IsNullOrEmpty(pictureUrl) || !string.IsNullOrEmpty(userInfo.Picture))
                            {
                                var userProfile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == newUser.Id);
                                if (userProfile != null)
                                {
                                    userProfile.ImageUrl = !string.IsNullOrEmpty(pictureUrl) ? pictureUrl : userInfo.Picture;
                                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                                }
                            }

                            authResult = await CreateSuccessAuthModel(newUser);
                            user = newUser;
                        }
                        else
                        {
                            throw new NotAuthenticatedException(tokenResult.Message);
                        }
                    }
                    else
                    {
                        // Account is valid, check if it was reactivated
                        var currentUser = await _userManager.FindByIdAsync(tokenResult.UserId);
                        if (currentUser != null && currentUser.LastReactivatedAt.HasValue && currentUser.LastReactivatedAt.Value > DateTime.UtcNow.AddDays(-30))
                        {
                            wasReactivated = true;
                            currentUser.LastReactivatedAt = null;
                            await _userManager.UpdateAsync(currentUser);
                        }

                        // Update user information if provided
                        if (!string.IsNullOrEmpty(firstName))
                            currentUser.FirstName = firstName;
                        if (!string.IsNullOrEmpty(lastName))
                            currentUser.LastName = lastName;
                        if (isEmailVerified)
                            currentUser.EmailConfirmed = true;

                        if (!string.IsNullOrEmpty(firstName))
                        {
                            currentUser.DisplayName = currentUser.FirstName;
                        }

                        await _userManager.UpdateAsync(currentUser);

                        // Update profile with picture URL only if user's current image is a Google image
                        if (!string.IsNullOrEmpty(pictureUrl) || !string.IsNullOrEmpty(userInfo.Picture))
                        {
                            var profilen = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                            if (profilen != null)
                            {
                                var shouldUpdateImage = !string.IsNullOrEmpty(profilen.ImageUrl) && IsGoogleProfileImage(profilen.ImageUrl);
                                
                                if (shouldUpdateImage)
                                {
                                    var newImageUrl = !string.IsNullOrEmpty(pictureUrl) ? pictureUrl : userInfo.Picture;
                                    _logger.LogInformation($"Updating profile image for user {currentUser.Id} from '{profilen.ImageUrl}' to '{newImageUrl}'");
                                    profilen.ImageUrl = newImageUrl;
                                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                                }
                                else
                                {
                                    _logger.LogInformation($"Skipping profile image update for user {currentUser.Id} - user has custom uploaded image or intentionally removed image: '{profilen.ImageUrl}'");
                                }
                            }
                        }

                        authResult = await CreateSuccessAuthModel(currentUser);
                        user = currentUser;
                    }
                }

                _logger.LogInformation($"User '{authResult.Username}' logged in with Google");

                var loginResponse = new LoginResponse()
                {
                    Id = authResult.UserId,
                    Roles = authResult.Roles,
                    Token = authResult.Token,
                    Username = authResult.Username,
                    Email = authResult.Email,
                    ImageUrl = null,
                    ShowWelcomeBack = wasReactivated
                };

                // Generate and store refresh token (store hash; send raw token to client)
                if (device != null && !string.IsNullOrEmpty(device.DeviceIdentifier))
                {
                    var refreshToken = GenerateSecureToken();
                    var refreshTokenEntity = new RefreshToken
                    {
                        UserId = user.Id,
                        Token = HashRefreshToken(refreshToken),
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        DeviceIdentifier = device.DeviceIdentifier
                    };
                    _dbContext.RefreshTokens.Add(refreshTokenEntity);
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    loginResponse.RefreshToken = refreshToken;
                }

                // Get profile information with proper includes
                try
                {
                    var profile = await _dbContext.Profiles
                        .Include(p => p.User)
                        .Include(p => p.Currency)
                        .Include(p => p.User.Stores)
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);

                    if (profile != null)
                    {
                        _logger.LogInformation($"Profile found for user {user.Id}");
                        loginResponse.ProfileUid = profile.Uid;
                        loginResponse.ImageUrl = profile.ImageUrl;
                        loginResponse.StoreUids = profile.User.Stores.Select(s => s.Uid).ToList();
                        loginResponse.FullName = profile.User.FirstName;
                        loginResponse.FirstName = profile.User.FirstName;
                        loginResponse.LastName = profile.User.LastName;
                        loginResponse.PhoneNumber = profile.User.PhoneNumber;
                        loginResponse.Currency = _mapper.Map<CurrencyDetailsResponse>(profile.Currency);
                    }
                    else
                    {
                        _logger.LogWarning($"No profile found for user {user.Id}");
                        loginResponse.FullName = user.FirstName;
                        loginResponse.FirstName = user.FirstName;
                        loginResponse.LastName = user.LastName;
                        loginResponse.PhoneNumber = user.PhoneNumber;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error retrieving profile for user {user.Id}");
                    loginResponse.FullName = user.FirstName;
                    loginResponse.FirstName = user.FirstName;
                    loginResponse.LastName = user.LastName;
                    loginResponse.PhoneNumber = user.PhoneNumber;
                }

                var profileEntity = await _dbContext.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == authResult.UserId);

                loginResponse.IsProfileComplete = profileEntity != null
                    && !string.IsNullOrWhiteSpace(profileEntity.User.FirstName)
                    && profileEntity.GenderId > 0
                    && !string.IsNullOrWhiteSpace(profileEntity.UserType);

                if (profileEntity != null)
                {
                    var onboardingPreferencesCount = await _dbContext.ProfileOnboardingPreferences
                        .Where(p => p.ProfileId == profileEntity.Id)
                        .CountAsync();
                    loginResponse.HasCompletedOnboarding = onboardingPreferencesCount > 0;
                }

                var (bagItemsCount, wishlistItemsCount, bagItemsTotalQuantity) = await GetUserCollectionCountsAsync(loginResponse.Id);
                loginResponse.BagItemsCount = bagItemsCount;
                loginResponse.WishlistItemsCount = wishlistItemsCount;
                loginResponse.BagItemsTotalQuantity = bagItemsTotalQuantity;

                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<LoginResponse> LoginWithAppleAsync(string identityToken, AppleNameInfo fullName = null, DeviceDto device = null)
        {
            try
            {
                // First validate the token (including expiry, signature, etc.)
                var isValidToken = await _appleAuthService.ValidateIdentityTokenAsync(identityToken);
                if (!isValidToken)
                {
                    throw new NotAuthenticatedException("Invalid or expired Apple identity token.");
                }

                var userInfo = await _appleAuthService.GetUserInfoAsync(identityToken);
                if (string.IsNullOrEmpty(userInfo.Email))
                {
                    throw new NotAuthenticatedException("Email not provided by Apple.");
                }

                var user = await _userManager.FindByEmailAsync(userInfo.Email);

                AuthModel authResult = null;
                bool wasReactivated = false;

                if (user == null)
                {
                    _logger.LogInformation("Creating new user from Apple Sign In");
                    
                    // For new users, use provided fullName or valid Apple name info
                    var nameInfo = fullName ?? userInfo.NameInfo;
                    
                    // Only use Apple name info if it's valid (not "String" or empty)
                    string firstName = "AppleUser";
                    string lastName = "User";
                    
                    if (nameInfo != null)
                    {
                        if (!string.IsNullOrWhiteSpace(nameInfo.GivenName) && !nameInfo.GivenName.Equals("String", StringComparison.OrdinalIgnoreCase))
                            firstName = nameInfo.GivenName;
                        if (!string.IsNullOrWhiteSpace(nameInfo.FamilyName) && !nameInfo.FamilyName.Equals("String", StringComparison.OrdinalIgnoreCase))
                            lastName = nameInfo.FamilyName;
                    }
                    
                    user = new User
                    {
                        UserName = userInfo.Email,
                        Email = userInfo.Email,
                        EmailConfirmed = userInfo.EmailVerified,
                        FirstName = firstName,
                        LastName = lastName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsSuspended = false
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError($"Failed to create user: {errors}");
                        throw new NotAuthenticatedException($"Failed to create user from Apple: {errors}");
                    }

                    // Add to User role
                    await _userManager.AddToRoleAsync(user, PulrRoles.User);

                    // Create profile for new user
                    // await _profileService.Create(user);

                    authResult = await CreateSuccessAuthModel(user);
                }
                else
                {
                    var tokenResult = await GetAuthModelForExternalLogin(user);
                    
                    if (!tokenResult.IsAuthenticated)
                    {
                        // If account is suspended beyond 30 days, create new user
                        if (tokenResult.Message == HttpErrorMessages.AccountSuspended)
                        {
                            // Update the suspended user's email to free up the email address
                            var timestamp = DateTime.UtcNow.Ticks;
                            user.Email = $"suspended_{timestamp}_{user.Email}";
                            user.UserName = $"suspended_{timestamp}_{user.UserName}";
                            await _userManager.UpdateAsync(user);
                            
                            // For suspended user recreation, use provided fullName or valid Apple name info
                            var nameInfo = fullName ?? userInfo.NameInfo;
                            
                            // Only use Apple name info if it's valid (not "String" or empty)
                            string firstName = "AppleUser";
                            string lastName = "User";
                            
                            if (nameInfo != null)
                            {
                                if (!string.IsNullOrWhiteSpace(nameInfo.GivenName) && !nameInfo.GivenName.Equals("String", StringComparison.OrdinalIgnoreCase))
                                    firstName = nameInfo.GivenName;
                                if (!string.IsNullOrWhiteSpace(nameInfo.FamilyName) && !nameInfo.FamilyName.Equals("String", StringComparison.OrdinalIgnoreCase))
                                    lastName = nameInfo.FamilyName;
                            }
                            
                            user = new User
                            {
                                UserName = userInfo.Email,
                                Email = userInfo.Email,
                                EmailConfirmed = userInfo.EmailVerified,
                                FirstName = firstName,
                                LastName = lastName,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                IsSuspended = false
                            };

                            var result = await _userManager.CreateAsync(user);
                            if (!result.Succeeded)
                            {
                                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                                _logger.LogError($"Failed to create user: {errors}");
                                throw new NotAuthenticatedException($"Failed to create user from Apple: {errors}");
                            }

                            // Add to User role
                            await _userManager.AddToRoleAsync(user, PulrRoles.User);

                            // Create profile for new user
                            // await _profileService.Create(user);

                            authResult = await CreateSuccessAuthModel(user);
                        }
                        else
                        {
                            throw new NotAuthenticatedException(tokenResult.Message);
                        }
                    }
                    else
                    {
                        // Account is valid, check if it was reactivated
                        var currentUser = await _userManager.FindByIdAsync(tokenResult.UserId);
                        if (currentUser != null && currentUser.LastReactivatedAt.HasValue && currentUser.LastReactivatedAt.Value > DateTime.UtcNow.AddDays(-30))
                        {
                            wasReactivated = true;
                            currentUser.LastReactivatedAt = null;
                            await _userManager.UpdateAsync(currentUser);
                        }

                        // For existing users, never update name from Apple - only use explicitly provided fullName
                        // This prevents Apple from overwriting user's manually set names
                        if (fullName != null)
                        {
                            if (!string.IsNullOrWhiteSpace(fullName.GivenName))
                                currentUser.FirstName = fullName.GivenName;
                            if (!string.IsNullOrWhiteSpace(fullName.FamilyName))
                                currentUser.LastName = fullName.FamilyName;
                            await _userManager.UpdateAsync(currentUser);
                        }

                        authResult = await CreateSuccessAuthModel(currentUser);
                        user = currentUser;
                    }
                }

                _logger.LogInformation($"User '{authResult.Username}' logged in with Apple");

                var loginResponse = new LoginResponse()
                {
                    Id = authResult.UserId,
                    Roles = authResult.Roles,
                    Token = authResult.Token,
                    Username = authResult.Username,
                    Email = authResult.Email,
                    ImageUrl = null,
                    ShowWelcomeBack = wasReactivated
                };

                // Generate and store refresh token (store hash; send raw token to client)
                if (device != null && !string.IsNullOrEmpty(device.DeviceIdentifier))
                {
                    var refreshToken = GenerateSecureToken();
                    var refreshTokenEntity = new RefreshToken
                    {
                        UserId = user.Id,
                        Token = HashRefreshToken(refreshToken),
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        DeviceIdentifier = device.DeviceIdentifier
                    };
                    _dbContext.RefreshTokens.Add(refreshTokenEntity);
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    loginResponse.RefreshToken = refreshToken;
                }

                try
                {
                    // Get profile information with a simpler query
                    var profile = await _dbContext.Profiles
                        .Include(p => p.User)
                        .Include(p => p.Currency)
                        .Include(p => p.User.Stores)
                        .FirstOrDefaultAsync(p => p.UserId == user.Id);

                    if (profile != null)
                    {
                        _logger.LogInformation($"Profile found for user {user.Id}");
                        loginResponse.ProfileUid = profile.Uid;
                        loginResponse.ImageUrl = profile.ImageUrl;
                        loginResponse.StoreUids = profile.User.Stores.Select(s => s.Uid).ToList();
                        loginResponse.FullName = profile.User.FirstName;
                        loginResponse.FirstName = profile.User.FirstName;
                        loginResponse.LastName = profile.User.LastName;
                        loginResponse.PhoneNumber = profile.User.PhoneNumber;
                        loginResponse.Currency = _mapper.Map<CurrencyDetailsResponse>(profile.Currency);
                    }
                    else
                    {
                        _logger.LogWarning($"No profile found for user {user.Id}");
                        // If no profile exists, use the user's information directly
                        loginResponse.FullName = user.FirstName;
                        loginResponse.FirstName = user.FirstName;
                        loginResponse.LastName = user.LastName;
                        loginResponse.PhoneNumber = user.PhoneNumber;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error retrieving profile for user {user.Id}");
                    // Continue with the login response even if profile retrieval fails
                    loginResponse.FullName = user.FirstName;
                    loginResponse.FirstName = user.FirstName;
                    loginResponse.LastName = user.LastName;
                    loginResponse.PhoneNumber = user.PhoneNumber;
                }

                var profileEntity = await _dbContext.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.UserId == authResult.UserId);

                loginResponse.IsProfileComplete = profileEntity != null
                    && !string.IsNullOrWhiteSpace(profileEntity.User.FirstName)
                    && profileEntity.GenderId > 0
                    && !string.IsNullOrWhiteSpace(profileEntity.UserType);

                // Check onboarding completion status
                if (profileEntity != null)
                {
                    var onboardingPreferencesCount = await _dbContext.ProfileOnboardingPreferences
                        .Where(p => p.ProfileId == profileEntity.Id)
                        .CountAsync();
                    loginResponse.HasCompletedOnboarding = onboardingPreferencesCount > 0;
                }

                var (bagItemsCount, wishlistItemsCount, bagItemsTotalQuantity) = await GetUserCollectionCountsAsync(loginResponse.Id);
                loginResponse.BagItemsCount = bagItemsCount;
                loginResponse.WishlistItemsCount = wishlistItemsCount;
                loginResponse.BagItemsTotalQuantity = bagItemsTotalQuantity;

                return loginResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<string> GenerateUniqueDisplayName(string firstName, string lastName)
        {
            try
            {
                // Generate a random number between 10000 and 99999
                Random random = new Random();
                int randomNumber = random.Next(10000, 99999);
                
                // Create base username from first name only
                string baseUsername = firstName.ToLower();
                
                // Remove any special characters and spaces
                baseUsername = new string(baseUsername.Where(c => char.IsLetterOrDigit(c)).ToArray());
                
                // Take first 6 characters if longer
                baseUsername = baseUsername.Length > 6 ? baseUsername.Substring(0, 6) : baseUsername;
                
                string displayName = $"@{baseUsername}{randomNumber}";
                bool displayNameExists = true;
                int attempts = 0;
                const int maxAttempts = 5;

                while (displayNameExists && attempts < maxAttempts)
                {
                    displayNameExists = await _dbContext.Users.AnyAsync(u => u.DisplayName == displayName);
                    if (!displayNameExists)
                    {
                        return displayName;
                    }
                    
                    // Generate new random number if display name exists
                    randomNumber = random.Next(10000, 99999);
                    displayName = $"@{baseUsername}{randomNumber}";
                    attempts++;
                }

                // If we couldn't find a unique name after max attempts, throw exception
                throw new Exception("Failed to generate unique display name after multiple attempts");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task DeactivateAccountAsync(User user)
        {
            try
            {
                // Set the user's profile as inactive
                var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.UserId == user.Id);
                if (profile != null)
                {
                    profile.IsActive = false;
                    // Note: Bookmarks are now handled through collections, no need to deactivate them separately

                    // Deactivate profile followers/followings
                    var followers = await _dbContext.ProfileFollowers.Where(f => f.ProfileId == profile.Id || f.FollowerId == profile.Id).ToListAsync();
                    foreach (var follower in followers)
                        follower.IsActive = false;

                    // Deactivate post my styles
                    var postMyStyles = await _dbContext.PostMyStyles.Where(pms => pms.ProfileId == profile.Id).ToListAsync();
                    foreach (var pms in postMyStyles)
                        pms.IsActive = false;

                    // Deactivate story likes
                    var storyLikes = await _dbContext.StoryLikes.Where(sl => sl.LikedById == profile.Id).ToListAsync();
                    foreach (var sl in storyLikes)
                        sl.IsActive = false;
                }

                // Set all user's stores as inactive
                var stores = await _dbContext.Stores.Where(s => s.UserId == user.Id).ToListAsync();
                foreach (var store in stores)
                {
                    store.IsActive = false;
                }

                // Set all user's posts as inactive
                var posts = await _dbContext.Posts.Where(p => p.User.Id == user.Id).ToListAsync();
                foreach (var post in posts)
                {
                    post.IsActive = false;
                }

                // Set all user's comments as inactive
                var comments = await _dbContext.Comments.Where(c => c.CommentedBy.UserId == user.Id).ToListAsync();
                foreach (var comment in comments)
                {
                    comment.IsActive = false;
                }

                // Set all user's stories as inactive
                var stories = await _dbContext.Stories.Where(s => s.UserId == user.Id).ToListAsync();
                foreach (var story in stories)
                {
                    story.IsActive = false;
                }

                // set all user's products as inactive
                var products = await _dbContext.Products.Where(p => p.UserId == user.Id).ToListAsync();
                foreach (var product in products)
                {
                    product.IsActive = false;
                }

                // Set all user's post likes as inactive
                var postLikes = await _dbContext.PostLikes.Where(pl => pl.LikedBy.User.Id == user.Id).ToListAsync();
                foreach (var pl in postLikes)
                    pl.IsActive = false;

                // Set all user's comment likes as inactive
                var commentLikes = await _dbContext.CommentLikes.Where(cl => cl.LikedBy.User.Id == user.Id).ToListAsync();
                foreach (var cl in commentLikes)
                    cl.IsActive = false;

                // Set all user's search history as inactive
                var searchHistories = await _dbContext.SearchHistories.Where(sh => sh.User.Id == user.Id).ToListAsync();
                foreach (var sh in searchHistories)
                    sh.IsActive = false;

                // Set all user's story views as inactive
                if (profile != null)
                {
                    var storyViews = await _dbContext.StorySeens.Where(sv => sv.SeenBy.Id == profile.Id).ToListAsync();
                    foreach (var sv in storyViews)
                        sv.IsActive = false;
                    profile.UpdatedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<List<LoginActivityDto>> GetLoginActivityAsync()
        {
            var userId = _currentUserService.GetUserId();
            var activities = await _dbContext.UserLoginActivities
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Select(a => new LoginActivityDto
                {
                    DeviceName = a.ModelName,
                    Action = a.Action,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();
            return activities;
        }

        public async Task<List<RecognisedDeviceDto>> GetRecognisedDevicesAsync()
        {
            var userId = _currentUserService.GetUserId();
            var activities = await _dbContext.UserLoginActivities
                .Where(a => a.UserId == userId)
                .Include(a => a.User)
                .ToListAsync();

            var devices = activities
                .GroupBy(a => a.DeviceIdentifier)
                .Select(g => g.OrderByDescending(a => a.Timestamp).FirstOrDefault())
                .Where(a => a != null && a.Action == "Logged in")
                .Select(a => new RecognisedDeviceDto
                {
                    DeviceName = a.ModelName,
                    DeviceIdentifier = a.DeviceIdentifier,
                    Username = a.User?.UserName
                })
                .ToList();
            return devices;
        }

        public async Task SaveLoginActivityAsync(string userId, string brand, string modelName, string osVersion, string deviceIdentifier, string appVersion, string action)
        {
            if (action == "Logged out")
            {
                // Find the latest 'Logged in' activity for this user and device
                var existing = await _dbContext.UserLoginActivities
                    .Where(a => a.UserId == userId && a.DeviceIdentifier == deviceIdentifier && a.Action == "Logged in")
                    .OrderByDescending(a => a.Timestamp)
                    .FirstOrDefaultAsync();
                if (existing != null)
                {
                    existing.Action = "Logged out";
                    existing.Timestamp = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    return;
                }
            }
            // Otherwise, add a new activity
            var activity = new UserLoginActivity
            {
                UserId = userId,
                Brand = brand,
                ModelName = modelName,
                OsVersion = osVersion,
                DeviceIdentifier = deviceIdentifier,
                AppVersion = appVersion,
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            _dbContext.UserLoginActivities.Add(activity);
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }

        public async Task SignOutDeviceAsync(string deviceIdentifier)
        {
            // Input validation and sanitization
            if (string.IsNullOrWhiteSpace(deviceIdentifier))
            {
                throw new ArgumentException("Device identifier is required");
            }

            // Additional sanitization - remove any potentially dangerous characters
            deviceIdentifier = deviceIdentifier.Trim();
            
            // Validate device identifier using SafeDeviceIdAttribute
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(new { DeviceIdentifier = deviceIdentifier });
            var attribute = new Core.Application.Security.Validation.Attributes.SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3);
            var validationResult = attribute.GetValidationResult(deviceIdentifier, validationContext);
            
            if (validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                throw new ArgumentException(validationResult.ErrorMessage);
            }

            var userId = _currentUserService.GetUserId();
            
            // Check if device identifier exists for this user
            var deviceExists = await _dbContext.UserLoginActivities
                .AnyAsync(a => a.UserId == userId && a.DeviceIdentifier == deviceIdentifier);
            
            if (!deviceExists)
            {
                throw new ArgumentException("device not available");
            }
            
            // Fetch the latest 'Logged in' activity for this user and device
            var latestLogin = await GetLatestLoginActivityAsync(userId, deviceIdentifier);
            if (latestLogin != null)
            {
                await SaveLoginActivityAsync(
                    userId,
                    latestLogin.Brand,
                    latestLogin.ModelName,
                    latestLogin.OsVersion,
                    deviceIdentifier,
                    latestLogin.AppVersion,
                    "Logged out"
                );
            }
            // Revoke all refresh tokens for this user and device
            var tokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.DeviceIdentifier == deviceIdentifier && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            // Clean up push tokens for the logged out device
            try
            {
                await _notificationService.CleanupPushTokensForLoggedOutDeviceAsync(userId, deviceIdentifier);
                _logger.LogInformation("Cleaned up push tokens for user {UserId} on device {DeviceIdentifier} during sign out", userId, deviceIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up push tokens for user {UserId} on device {DeviceIdentifier} during sign out", userId, deviceIdentifier);
            }
        }

        public async Task SignOutAllDevicesAsync(string currentDeviceIdentifier)
        {
            // Input validation and sanitization
            ValidateDeviceIdentifier(currentDeviceIdentifier);

            var userId = _currentUserService.GetUserId();

            // Check the device identifier exists for the user
            var deviceExists = await _dbContext.UserLoginActivities
                .AnyAsync(a => a.UserId == userId && a.DeviceIdentifier == currentDeviceIdentifier);
            if (!deviceExists)
            {
                throw new BadRequestException("Device identifier not found for this user");
            }

            var devices = await _dbContext.UserLoginActivities
                .Where(a => a.UserId == userId)
                .ToListAsync();

            if (devices.Count == 0)
            {
                throw new BadRequestException("No devices found for this user");
            }

            var latestLogins = devices
                .GroupBy(a => a.DeviceIdentifier)
                .Select(g => g.OrderByDescending(a => a.Timestamp).FirstOrDefault())
                .Where(a => a.Action == "Logged in")
                .ToList();
            
            foreach (var device in latestLogins)
            {
                if (device.DeviceIdentifier != currentDeviceIdentifier)
                {
                    await SaveLoginActivityAsync(
                        userId,
                        device.Brand,
                        device.ModelName,
                        device.OsVersion,
                        device.DeviceIdentifier,
                        device.AppVersion,
                        "Logged out"
                    );

                    // Clean up push tokens for each signed out device
                    try
                    {
                        await _notificationService.CleanupPushTokensForLoggedOutDeviceAsync(userId, device.DeviceIdentifier);
                        _logger.LogInformation("Cleaned up push tokens for user {UserId} on device {DeviceIdentifier} during sign out all", userId, device.DeviceIdentifier);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning up push tokens for user {UserId} on device {DeviceIdentifier} during sign out all", userId, device.DeviceIdentifier);
                    }
                }
            }
            // Revoke all refresh tokens for this user except the current device
            var tokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.DeviceIdentifier != currentDeviceIdentifier && rt.RevokedAt == null)
                .ToListAsync();
            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }

        public async Task<UserNotificationSettingDto> GetNotificationSettingsAsync(string deviceId, string pushToken)
        {
            // Input validation and sanitization
            ValidateDeviceIdAndPushToken(deviceId, pushToken);

            var userId = _currentUserService.GetUserId();
            var settings = await _dbContext.UserNotificationSettings
                .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId && x.PushToken == pushToken);
            
            if (settings == null)
            {
                // Return default settings if not set
                //return new UserNotificationSettingDto
                //{
                //    DeviceId = deviceId,
                //    PushToken = pushToken,
                //    Likes = true,
                //    Comments = true,
                //    Mentions = true,
                //    Follows = true,
                //    SavedPosts = true,
                //    ShopActivity = true,
                //    DirectMessages = true,
                //    EmailNotification = true
                //};
                // If no settings found, return message
                throw new NotFoundException("Notification settings not found for this device.");


            }

            return new UserNotificationSettingDto
            {
                DeviceId = settings.DeviceId,
                PushToken = settings.PushToken,
                Likes = settings.Likes,
                Comments = settings.Comments,
                Mentions = settings.Mentions,
                Follows = settings.Follows,
                SavedPosts = settings.SavedPosts,
                ShopActivity = settings.ShopActivity,
                DirectMessages = settings.DirectMessages,
                EmailNotification = settings.EmailNotification
            };
        }

        public async Task<UserNotificationSettingDto> UpdateNotificationSettingsAsync(UserNotificationSettingDto dto)
        {
            var userId = _currentUserService.GetUserId();
            var settings = await _dbContext.UserNotificationSettings
                .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == dto.DeviceId && x.PushToken == dto.PushToken);

            if (settings == null)
            {
                settings = new UserNotificationSetting
                {
                    UserId = userId,
                    DeviceId = dto.DeviceId,
                    PushToken = dto.PushToken,
                    Likes = dto.Likes ?? true,
                    Comments = dto.Comments ?? true,
                    Mentions = dto.Mentions ?? true,
                    Follows = dto.Follows ?? true,
                    SavedPosts = dto.SavedPosts ?? true,
                    ShopActivity = dto.ShopActivity ?? true,
                    DirectMessages = dto.DirectMessages ?? true,
                    EmailNotification = dto.EmailNotification ?? true
                };
                _dbContext.UserNotificationSettings.Add(settings);
            }
            else
            {
                // Update only the fields that are provided (not null)
                if (dto.Likes.HasValue) settings.Likes = dto.Likes.Value;
                if (dto.Comments.HasValue) settings.Comments = dto.Comments.Value;
                if (dto.Mentions.HasValue) settings.Mentions = dto.Mentions.Value;
                if (dto.Follows.HasValue) settings.Follows = dto.Follows.Value;
                if (dto.SavedPosts.HasValue) settings.SavedPosts = dto.SavedPosts.Value;
                if (dto.ShopActivity.HasValue) settings.ShopActivity = dto.ShopActivity.Value;
                if (dto.DirectMessages.HasValue) settings.DirectMessages = dto.DirectMessages.Value;
                if (dto.EmailNotification.HasValue) settings.EmailNotification = dto.EmailNotification.Value;
            }
            
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            return new UserNotificationSettingDto
            {
                DeviceId = settings.DeviceId,
                PushToken = settings.PushToken,
                Likes = settings.Likes,
                Comments = settings.Comments,
                Mentions = settings.Mentions,
                Follows = settings.Follows,
                SavedPosts = settings.SavedPosts,
                ShopActivity = settings.ShopActivity,
                DirectMessages = settings.DirectMessages,
                EmailNotification = settings.EmailNotification
            };
        }

        public async Task CreateNotificationSettingsForDeviceAsync(string deviceId, string pushToken)
        {
            var userId = _currentUserService.GetUserId();
            
            // Check if notification settings already exist for this device
            var existingSettings = await _dbContext.UserNotificationSettings
                .FirstOrDefaultAsync(x => x.UserId == userId && x.DeviceId == deviceId && x.PushToken == pushToken);
            
            if (existingSettings == null)
            {
                // Create new notification settings for this device
                var settings = new UserNotificationSetting
                {
                    UserId = userId,
                    DeviceId = deviceId,
                    PushToken = pushToken,
                    Likes = true,
                    Comments = true,
                    Mentions = true,
                    Follows = true,
                    SavedPosts = true,
                    ShopActivity = true,
                    DirectMessages = true,
                    EmailNotification = true
                };
                
                _dbContext.UserNotificationSettings.Add(settings);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken, string deviceIdentifier)
        {
            var tokenHash = HashRefreshToken(refreshToken);
            var tokenEntity = await _dbContext.RefreshTokens.Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == tokenHash && rt.RevokedAt == null);
            var deviceExists = await _dbContext.RefreshTokens
                .AnyAsync(a => a.DeviceIdentifier == deviceIdentifier && a.Token == tokenHash);
            if (!deviceExists)
            {
                throw new NotAuthenticatedException("Invalid device identifier");
            }

            if (tokenEntity == null || tokenEntity.IsExpired || !tokenEntity.IsActive)
            {
                throw new NotAuthenticatedException("Invalid or expired refresh token");
            }

            var user = tokenEntity.User;
            if (user == null)
            {
                throw new NotAuthenticatedException("User not found for this refresh token");
            }

            // Generate new token, store hash, return raw token to client
            var newRefreshToken = GenerateSecureToken();
            tokenEntity.ReplacedByToken = tokenEntity.Token;
            tokenEntity.Token = HashRefreshToken(newRefreshToken);
            tokenEntity.CreatedAt = DateTime.UtcNow;
            tokenEntity.ExpiresAt = DateTime.UtcNow.AddDays(90);
            tokenEntity.DeviceIdentifier = deviceIdentifier;
            tokenEntity.RevokedAt = null;

            await _dbContext.SaveChangesAsync(CancellationToken.None);

            // Issue new JWT
            var jwt = await CreateJwtToken(user);
            var rolesList = await _userManager.GetRolesAsync(user);

            var profile = await _dbContext.Profiles
                .Where(p => p.UserId == user.Id)
                .Select(p => new LoginResponse
                {
                    ProfileUid = p.Uid,
                    FullName = p.User.FirstName,
                    FirstName = p.User.FirstName,
                    LastName = p.User.LastName,
                    Username = p.User.UserName,
                    ImageUrl = p.ImageUrl,
                    Currency = _mapper.Map<CurrencyDetailsResponse>(p.Currency),
                    StoreUids = p.User.Stores.Select(s => s.Uid).ToList()
                }).SingleOrDefaultAsync();

            var loginResponse = new LoginResponse
            {
                Id = user.Id,
                Roles = rolesList.ToList(),
                Token = new JwtSecurityTokenHandler().WriteToken(jwt),
                Username = user.UserName,
                Email = user.Email,
                ImageUrl = profile?.ImageUrl,
                ProfileUid = profile?.ProfileUid,
                FullName = profile?.FullName,
                FirstName = profile?.FirstName,
                LastName = profile?.LastName,
                PhoneNumber = user.PhoneNumber,
                StoreUids = profile?.StoreUids,
                Currency = profile?.Currency,
                RefreshToken = newRefreshToken
            };

            // Set unread notification count
            var userProfile = await _dbContext.Profiles
                .Where(p => p.UserId == user.Id)
                .Select(p => new { p.Id })
                .SingleOrDefaultAsync();
            var unreadCount = await _dbContext.NotificationHistories
                .Where(n => n.ReceiverUserId == userProfile.Id && !n.IsRead)
                .CountAsync();
            loginResponse.UnreadNotificationCount = unreadCount;

            var (bagItemsCount, wishlistItemsCount, bagItemsTotalQuantity) = await GetUserCollectionCountsAsync(user.Id);
            loginResponse.BagItemsCount = bagItemsCount;
            loginResponse.WishlistItemsCount = wishlistItemsCount;
            loginResponse.BagItemsTotalQuantity = bagItemsTotalQuantity;

            return loginResponse;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var tokenEntity = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.RevokedAt == null);
            if (tokenEntity != null)
            {
                tokenEntity.RevokedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
        }

        public static string GetJtiFromToken(string token)
        {
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken?.Id;
            }
            catch (Exception)
            {
                // Return null for any JWT-related exceptions (malformed, expired, etc.)
                return null;
            }
        }

        /// <summary>
        /// Reads the expiry (UTC) of a JWT so a revoked token can be blacklisted
        /// only until it would have expired on its own. Falls back to "now" on any
        /// parse failure so a malformed token is not retained indefinitely.
        /// </summary>
        public static DateTime GetTokenExpiryUtc(string token)
        {
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken?.ValidTo ?? DateTime.UtcNow;
            }
            catch (Exception)
            {
                return DateTime.UtcNow;
            }
        }

        public async Task<UserLoginActivity> GetLatestLoginActivityAsync(string userId, string deviceIdentifier)
        {
            return await _dbContext.UserLoginActivities
                .Where(a => a.UserId == userId && a.DeviceIdentifier == deviceIdentifier && a.Action == "Logged in")
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Cleans up all push tokens for a specific user (useful when user is deactivated, suspended, or deleted)
        /// </summary>
        /// <param name="userId">The user ID to clean up all tokens for</param>
        public async Task CleanupAllPushTokensForUserAsync(string userId)
        {
            try
            {
                // Get all devices for this user
                var userDevices = await _dbContext.UserLoginActivities
                    .Where(a => a.UserId == userId)
                    .Select(a => a.DeviceIdentifier)
                    .Distinct()
                    .ToListAsync();

                var cleanedCount = 0;
                foreach (var deviceId in userDevices)
                {
                    try
                    {
                        await _notificationService.CleanupPushTokensForLoggedOutDeviceAsync(userId, deviceId);
                        cleanedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning up push tokens for user {UserId} on device {DeviceId}", userId, deviceId);
                    }
                }

                _logger.LogInformation("Cleaned up push tokens for user {UserId} across {DeviceCount} devices", userId, cleanedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up all push tokens for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Validates device identifier parameter
        /// </summary>
        /// <param name="deviceIdentifier">The device identifier to validate</param>
        private void ValidateDeviceIdentifier(string deviceIdentifier)
        {
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(new { DeviceIdentifier = deviceIdentifier });
            var attribute = new Core.Application.Security.Validation.Attributes.SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3);
            var validationResult = attribute.GetValidationResult(deviceIdentifier, validationContext);
            
            if (validationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                throw new BadRequestException(validationResult.ErrorMessage);
            }
        }

        /// <summary>
        /// Validates device ID and push token parameters
        /// </summary>
        /// <param name="deviceId">The device ID to validate</param>
        /// <param name="pushToken">The push token to validate</param>
        private void ValidateDeviceIdAndPushToken(string deviceId, string pushToken)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new BadRequestException("Device ID is required");

            if (string.IsNullOrWhiteSpace(pushToken))
                throw new BadRequestException("Push Token is required");

            // Validate device ID format and length using SafeDeviceIdAttribute
            var deviceValidationContext = new System.ComponentModel.DataAnnotations.ValidationContext(new { DeviceId = deviceId });
            var deviceAttribute = new Core.Application.Security.Validation.Attributes.SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3);
            var deviceValidationResult = deviceAttribute.GetValidationResult(deviceId, deviceValidationContext);
            
            if (deviceValidationResult != System.ComponentModel.DataAnnotations.ValidationResult.Success)
            {
                throw new BadRequestException(deviceValidationResult.ErrorMessage);
            }

            // Validate push token format and length
            if (!IsValidExpoPushToken(pushToken))
                throw new BadRequestException("Invalid push token format");

            if (pushToken.Length > 200)
                throw new BadRequestException("Push token cannot exceed 200 characters");
        }

        /// <summary>
        /// Validates if the provided token is a valid Expo push token format
        /// </summary>
        /// <param name="pushToken">The push token to validate</param>
        /// <returns>True if the token is in valid Expo format, false otherwise</returns>
        private bool IsValidExpoPushToken(string pushToken)
        {
            if (string.IsNullOrEmpty(pushToken))
                return false;

            // Basic Expo token validation - should start with ExponentPushToken or ExpoPushToken
            return pushToken.StartsWith("ExponentPushToken[") || 
                   pushToken.StartsWith("ExpoPushToken[") ||
                   pushToken.StartsWith("ExpoToken[");
        }

        /// <summary>
        /// Determines if the provided image URL is from Google's profile image service
        /// </summary>
        /// <param name="imageUrl">The image URL to check</param>
        /// <returns>True if the URL appears to be from Google, false otherwise</returns>
        private bool IsGoogleProfileImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return false;

            // Check if the URL contains Google's image service domains
            return imageUrl.Contains("googleapis.com") || 
                   imageUrl.Contains("googleusercontent.com") ||
                   imageUrl.Contains("lh3.googleusercontent.com") ||
                   imageUrl.Contains("lh4.googleusercontent.com") ||
                   imageUrl.Contains("lh5.googleusercontent.com") ||
                   imageUrl.Contains("lh6.googleusercontent.com");
        }

    }
}