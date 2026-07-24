using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Mediatr.Users.Commands.Delete; 
using Core.Application.Mediatr.Users.Commands.Login;
using Core.Application.Mediatr.Users.Commands.Password;
using Core.Application.Mediatr.Users.Commands.Register;
using Core.Application.Mediatr.Users.Queries;
using Core.Application.Models.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using System;
using System.Collections.Generic;
using Core.Application.Exceptions;
using Core.Application.Mediatr.Users.Commands;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using Core.Application.Helpers;
using Core.Application.Security.Validation.Attributes;
using WebApi.Utilities;
using Microsoft.AspNetCore.Identity;
using Core.Domain.Entities;
using Core.Application.Mediatr.Users.Commands.Deactivate;
using Core.Application.Mediatr.Users.Commands.AdminDelete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers
{
    public class UsersController : ApiControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsersController> _logger;
        private readonly IUserService _userService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public UsersController(
            IConfiguration configuration, 
            ILogger<UsersController> logger,
            IUserService userService,
            ITokenBlacklistService tokenBlacklistService,
            UserManager<User> userManager,
            INotificationService notificationService
            )
        {
            _configuration = configuration;
            _logger = logger;
            _userService = userService;
            _tokenBlacklistService = tokenBlacklistService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginCommand command)
        {
            try
        {
            var res = await Mediator.Send(command);
                return Ok(res);

            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during login");
                return BadRequest(new { message = ex.Message, errors = ex.Errors });
            }
            catch (NotAuthenticatedException ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(401, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "An error occurred while logging in" });
            }
        }

        //[AllowAnonymous]
        //[HttpPost("login-facebook")]
        //public async Task<ActionResult<LoginResponse>> FacebookLogin(FacebookLoginCommand command)
        //{
        //    var res = await Mediator.Send(command);
        //    return Ok(res);
        //}

        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("login-google-token")]
        public async Task<ActionResult<LoginResponse>> GoogleLoginWithToken(GoogleLoginCommand command)
        {
            var res = await Mediator.Send(command);
            return Ok(res);
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("login-apple")]
        public async Task<ActionResult<LoginResponse>> AppleLogin(AppleLoginCommand command)
        {
            try
            {
                var res = await Mediator.Send(command);
                return Ok(res);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during Apple login");
                return BadRequest(new { message = ex.Message, errors = ex.Errors });
            }
            catch (SecurityTokenMalformedException ex)
            {
                _logger.LogWarning(ex, "Malformed JWT token during Apple login");
                return StatusCode(401, new { message = "Invalid token format" });
            }
            catch (NotAuthenticatedException ex)
            {
                _logger.LogError(ex, "Authentication error during Apple login");
                return StatusCode(401, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Apple login");

                // Fallback mapping for wrapped token validation errors
                var message = ex.Message ?? string.Empty;
                var innerMessage = ex.InnerException?.Message ?? string.Empty;
                var combined = message + " " + innerMessage;

                if (combined.IndexOf("IDX10223", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return StatusCode(401, new { message = "Apple identity token has expired. Please sign in again." });
                }
                if (combined.IndexOf("IDX10511", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return StatusCode(401, new { message = "Invalid Apple identity token." });
                }

                return StatusCode(500, new { message = "An error occurred while logging in" });
            }
        }

        [HttpGet("data")]
        public async Task<ActionResult<LoginResponse>> GetCurrentUserDataQuery()
        {
            var res = await Mediator.Send(new GetCurrentUserDataQuery());
            return Ok(res);
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth-otp-send")]
        [HttpPost("forgot-password")]
        public async Task<ActionResult> PasswordResetRequest([FromBody] PasswordResetRequestCommand command)
        {
            try
            {
                await Mediator.Send(command);
                return Ok();
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during password reset request for email: {Email}", command.Email);
                return BadRequest(new { message = "Invalid email format" });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found during password reset request for email: {Email}", command.Email);
                return BadRequest(new { message = "Invalid email format" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request for email: {Email}", command.Email);
                return BadRequest(new { message = "Invalid email format" });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth-otp-verify")]
        [HttpPost("verify-otp")]
        public async Task<ActionResult<bool>> VerifyOtp([FromBody] VerifyOtpCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(result);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP");
                return StatusCode(500, new { message = "An error occurred while verifying the OTP" });
            }
        }

        [HttpPost("send-email-verification-otp")]
        [AllowAnonymous]
        [EnableRateLimiting("auth-otp-send")]
        public async Task<ActionResult<EmailVerificationResponse>> SendEmailVerificationOtp([FromBody] SendEmailVerificationOtpCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email verification OTP");
                return StatusCode(500, new { Message = "An error occurred while sending the verification OTP." });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth-otp-verify")]
        [HttpPost("change-password-from-email")]
        public async Task<ActionResult> ChangePasswordFromEmail([FromBody] ChangePasswordFromEmailCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth-register")]
        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterCommand command)
        {
            await Mediator.Send(command);
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("confirm-email")]
        public async Task<ActionResult> ConfirmEmail([FromQuery] ConfirmEmailCommand command)
        {
            try
            {
                await Mediator.Send(command);
                return Ok(new { message = "Email confirmed successfully" });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email");
                return StatusCode(500, new { message = "An error occurred while confirming your email" });
            }
        }

        [AllowAnonymous]
        [HttpPost("check-username")]
        public async Task<ActionResult<CheckUsernameResponse>> CheckUsername([FromBody] CheckUsernameRequest request)
        {
            // Check if model state is valid (validation attributes will be checked automatically)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(new { Message = string.Join("; ", errors) });
            }

            var normalization = UsernameHelper.Normalize(request.Username);
            var User = await _userManager.FindByNameAsync(normalization);

            return Ok(new CheckUsernameResponse
            {
                Exists = User != null,
                Message = User != null ? "Username already exists" : "Username is available"
            });
        }

        [HttpDelete]
        public async Task<ActionResult> Delete()
        {
            await Mediator.Send(new DeleteUserCommand());
            return NoContent();
        }

        [AllowAnonymous]
        [HttpGet("check-email")]
        public async Task<ActionResult<CheckEmailResponse>> CheckEmail([FromQuery] CheckEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid email format" });
            }

            var response = await Mediator.Send(new CheckEmailQuery { Email = request.Email });
            return Ok(response);
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("accept-terms")]
        public async Task<ActionResult<bool>> AcceptTerms([FromBody] AcceptTermsCommand command)
        {
            try
            {
                var result = await Mediator.Send(command);
                return Ok(result);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = "Invalid email format", errors = ex.Errors });
            }
            catch (NotAuthenticatedException ex)
            {
                return StatusCode(401, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting terms");
                return StatusCode(500, new { message = "An error occurred while accepting terms" });
            }
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("block")]
        public async Task<ActionResult<BlockUserResponse>> BlockUser([FromBody] BlockUserCommand command)
        {
            try
            {
                var response = await Mediator.Send(command);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize(Roles = PulrRoles.User)]
        [HttpPost("unblock")]
        public async Task<ActionResult<UnblockUserResponse>> UnblockUser([FromBody] UnblockUserCommand command)
        {
            try
            {
                var response = await Mediator.Send(command);
                return Ok(response);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] LogoutRequest request)
        {
            // Validate model state for required fields
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(new { message = "Validation failed", errors = errors });
            }

            var token = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            
            if (token != null)
            {
                // Blacklist the JWT's jti until the token's own expiry, so the
                // revocation persists across restarts and across instances.
                var jti = Core.Infrastructure.Services.Users.UserService.GetJtiFromToken(token);
                if (!string.IsNullOrEmpty(jti))
                {
                    var expiresAtUtc = Core.Infrastructure.Services.Users.UserService.GetTokenExpiryUtc(token);
                    await _tokenBlacklistService.BlacklistTokenAsync(jti, expiresAtUtc);
                }
            }

            // Revoke refresh token (now required)
            await _userService.RevokeRefreshTokenAsync(request.RefreshToken);

            // Save logout activity for the device (now required)
            var userId = _userManager.GetUserId(User);
            // Fetch the latest 'Logged in' activity for this user and device
            var latestLogin = await _userService.GetLatestLoginActivityAsync(userId, request.DeviceIdentifier);
            await _userService.SaveLoginActivityAsync(
                userId,
                latestLogin?.Brand,
                latestLogin?.ModelName,
                latestLogin?.OsVersion,
                request.DeviceIdentifier,
                latestLogin?.AppVersion,
                "Logged out"
            );

            // Delete push token for the device
            try
            {
                await _notificationService.DeletePushTokenAsync(userId, request.DeviceIdentifier);
                _logger.LogInformation("Push token deleted for user {UserId} on device {DeviceId} during logout", userId, request.DeviceIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete push token for user {UserId} on device {DeviceId} during logout", userId, request.DeviceIdentifier);
                // Don't throw here as logout should still succeed even if push token deletion fails
            }

            return Ok(new { message = "Successfully logged out" });
        }

        [HttpPost("deactivate")]
        [Authorize(Roles = PulrRoles.User)]
        public async Task<ActionResult> DeactivateAccount()
        {
            await Mediator.Send(new DeactivateUserCommand());
            return Ok(new { message = "Your account has been deactivated. You can reactivate it by logging back in." });
        }

        [Authorize]
        [HttpGet("login-activity")]
        public async Task<ActionResult<List<LoginActivityDto>>> GetLoginActivity()
        {
            var activities = await _userService.GetLoginActivityAsync();
            return Ok(activities);
        }

        [Authorize]
        [HttpGet("recognised-devices")]
        public async Task<ActionResult<List<RecognisedDeviceDto>>> GetRecognisedDevices()
        {
            var devices = await _userService.GetRecognisedDevicesAsync();
            return Ok(devices);
        }

        [Authorize]
        [HttpPost("signout-device")]
        public async Task<ActionResult> SignOutDevice([FromBody] SignOutDeviceRequest request)
        {
            // Check if model state is valid (validation attributes will be checked automatically)
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(new { Message = "Invalid input data", Errors = errors });
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                await _userService.SignOutDeviceAsync(request.DeviceIdentifier);
                
                // Delete push token for the device
                try
                {
                    await _notificationService.DeletePushTokenAsync(userId, request.DeviceIdentifier);
                    _logger.LogInformation("Push token deleted for user {UserId} on device {DeviceId} during signout", userId, request.DeviceIdentifier);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete push token for user {UserId} on device {DeviceId} during signout", userId, request.DeviceIdentifier);
                    // Don't throw here as signout should still succeed even if push token deletion fails
                }
                
                return Ok(new { Message = "Device signed out successfully" });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid device identifier provided");
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing out device");
                return StatusCode(500, new { Message = "An error occurred while signing out the device" });
            }
        }

        [Authorize]
        [HttpPost("signout-all-devices")]
        public async Task<ActionResult> SignOutAllDevices([FromBody] SignOutAllDevicesRequest request)
        {
            // Validate model state first
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return BadRequest(new { Message = "Validation failed", Errors = errors });
            }

            // Additional security check using SafeDeviceIdAttribute
            var deviceIdValidationError = this.ValidateWithAttribute(
                request.CurrentDeviceIdentifier,
                new SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3),
                memberName: "CurrentDeviceIdentifier",
                statusCode: 400);
            if (deviceIdValidationError != null) return deviceIdValidationError;

            try
            {
                var userId = _userManager.GetUserId(User);
                await _userService.SignOutAllDevicesAsync(request.CurrentDeviceIdentifier);
                
                // Clean up all push tokens for the user
                try
                {
                    await _userService.CleanupAllPushTokensForUserAsync(userId);
                    _logger.LogInformation("All push tokens cleaned up for user {UserId} during signout all devices", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up push tokens for user {UserId} during signout all devices", userId);
                    // Don't throw here as signout should still succeed even if push token cleanup fails
                }
                
                return Ok(new { Message = "Successfully signed out all devices" });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing out all devices for user");
                return StatusCode(500, new { Message = "An error occurred while signing out all devices" });
            }
        }

        /// <summary>
        /// Checks if the input contains potentially malicious content like XSS or SQL injection patterns
        /// </summary>
        /// <param name="input">The input string to check</param>
        /// <returns>True if malicious content is detected, false otherwise</returns>
        private bool ContainsMaliciousContent(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Check for common XSS patterns
            var xssPatterns = new[]
            {
                @"<script", @"</script", @"javascript:", @"on\w+\s*=", @"<iframe", @"<object",
                @"<embed", @"<link", @"<meta", @"<style", @"<img.*onerror", @"alert\s*\(",
                @"document\.", @"window\.", @"eval\s*\(", @"expression\s*\("
            };

            // Check for SQL injection patterns
            var sqlPatterns = new[]
            {
                @"union\s+select", @"drop\s+table", @"delete\s+from", @"insert\s+into",
                @"update\s+set", @"exec\s*\(", @"execute\s*\(", @"sp_\w+", @"xp_\w+",
                @"';\s*--", @"'\s*;\s*drop", @"'\s*;\s*delete", @"'\s*;\s*update",
                @"'\s*;\s*insert", @"'\s*;\s*exec", @"'\s*;\s*execute"
            };

            var allPatterns = xssPatterns.Concat(sqlPatterns);

            foreach (var pattern in allPatterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }


        [Authorize]
        [HttpGet("notification-settings")]
        public async Task<ActionResult<UserNotificationSettingDto>> GetNotificationSettings([FromQuery] string deviceId, [FromQuery] string pushToken)
        {
            // Basic validation - detailed validation is handled in service layer
            if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(pushToken))
            {
                return BadRequest(new { Message = "DeviceId and PushToken are required" });
            }

            // Validate device ID using SafeDeviceIdAttribute
            var deviceIdValidationError = this.ValidateWithAttribute(
                deviceId,
                new SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3),
                memberName: "DeviceId",
                statusCode: 400);
            if (deviceIdValidationError != null) return deviceIdValidationError;

            try
            {
                var settings = await _userService.GetNotificationSettingsAsync(deviceId, pushToken);
                return Ok(settings);
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("notification-settings")]
        public async Task<ActionResult<UserNotificationSettingDto>> UpdateNotificationSettings([FromBody] UserNotificationSettingDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.DeviceId) || string.IsNullOrWhiteSpace(dto.PushToken))
            {
                return BadRequest(new { message = "DeviceId and PushToken are required" });
            }
            if (!IsValidDeviceId(dto.DeviceId) || !IsValidPushToken(dto.PushToken))
            {
                return BadRequest(new { message = "Invalid DeviceId or PushToken format" });
            }
            var updatedSettings = await _userService.UpdateNotificationSettingsAsync(dto);
            return Ok(updatedSettings);
        }

        [Authorize]
        [HttpPost("notification-settings/device")]
        public async Task<ActionResult> CreateNotificationSettingsForDevice([FromBody] CreateNotificationSettingsRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.DeviceId) || string.IsNullOrWhiteSpace(request.PushToken))
            {
                return BadRequest(new { message = "DeviceId and PushToken are required" });
            }
            if (!IsValidDeviceId(request.DeviceId) || !IsValidPushToken(request.PushToken))
            {
                return BadRequest(new { message = "Invalid DeviceId or PushToken format" });
            }
            await _userService.CreateNotificationSettingsForDeviceAsync(request.DeviceId, request.PushToken);
            return Ok();
        }

        private bool IsValidDeviceId(string deviceId)
        {
            var deviceIdValidationError = this.ValidateWithAttribute(
                deviceId,
                new SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3),
                memberName: "DeviceId",
                statusCode: 400);
            return deviceIdValidationError == null;
        }

        private bool IsValidPushToken(string pushToken)
        {
            if (string.IsNullOrWhiteSpace(pushToken) || pushToken.Length < 10 || pushToken.Length > 512)
            {
                return false;
            }
            return System.Text.RegularExpressions.Regex.IsMatch(pushToken, @"^[A-Za-z0-9_:.-\[\]\{\}=+\./@]+$");
        }

        [Authorize]
        [HttpGet("blocked-users")]
        public async Task<ActionResult<List<BlockedUserDto>>> GetBlockedUsers()
        {
            try
            {
                var blockedUsers = await Mediator.Send(new GetBlockedUsersQuery());
                return Ok(blockedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blocked users");
                return StatusCode(500, new { Message = "An error occurred while getting blocked users." });
            }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth-refresh")]
        [HttpPost("refresh-token")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if(!IsValidDeviceId(request.DeviceIdentifier))
            {
                return BadRequest(new { message = "Invalid DeviceIdentifier format" });
            }
            try
            {
                var response = await _userService.RefreshTokenAsync(request.RefreshToken, request.DeviceIdentifier);
                return Ok(response);
            }
            catch (NotAuthenticatedException ex)
            {
                _logger.LogError(ex, "Error during refresh token");
                return StatusCode(401, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refresh token");
                return StatusCode(500, new { message = "An error occurred while refreshing token" });
            }
        }

        [HttpDelete("admin-delete-user")]
        [AllowAnonymous] // You may want to restrict this further in production
        public async Task<IActionResult> AdminDeleteUser([FromBody] AdminDeleteUserRequest request)
        {
            try
            {
                var command = new AdminDeleteUserCommand
                {
                    Username = request.Username,
                    SecretCode = request.SecretCode
                };

                var result = await Mediator.Send(command);

                if (!result.Success)
                {
                    if (result.Message.Contains("Invalid secret code"))
                    {
                        return Unauthorized(new { message = result.Message });
                    }
                    else if (result.Message.Contains("not found"))
                    {
                        return NotFound(new { message = result.Message });
                    }
                    else
                    {
                        return StatusCode(500, new { message = result.Message });
                    }
                }

                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AdminDeleteUser endpoint. TraceId: {TraceId}", HttpContext.TraceIdentifier);
                return StatusCode(500, new { message = "An unexpected error occurred.", traceId = HttpContext.TraceIdentifier });
            }
        }

    }
}
