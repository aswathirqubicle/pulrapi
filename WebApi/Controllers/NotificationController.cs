using Core.Application.Interfaces;
using Core.Application.Models.Users;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WebApi.Utilities;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController(
        INotificationService notificationService,
        ICurrentUserService currentUserService,
        IUserService userService) : ControllerBase
    {
        private readonly INotificationService _notificationService = notificationService;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IUserService _userService = userService;

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var currentUserId = _currentUserService.GetUserId();
            var notifications = await _notificationService.GetNotificationsAsync(currentUserId, page, pageSize);
            return Ok(notifications);
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUserId = _currentUserService.GetUserId();
            await _notificationService.MarkAllNotificationsAsReadAsync(currentUserId);
            
            // Return the updated unread count (should be 0 after marking all as read)
            var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(currentUserId);
            return Ok(new { unreadCount });
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = _currentUserService.GetUserId();
            var unreadCount = await _notificationService.GetUnreadNotificationCountAsync(currentUserId);
            return Ok(new { unreadCount });
        }

        [HttpPost("mention")]
        public async Task<IActionResult> SaveMentionNotification([FromBody] MentionNotificationRequest request)
        {
            // Validate request is not null
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            // Validate MentionType enum value
            if (!Enum.TryParse<MentionTypeEnum>(request.MentionType, true, out var mentionType))
            {
                return BadRequest(new { message = "Invalid MentionType value" });
            }

            var currentUserId = _currentUserService.GetUserId();
            await _notificationService.SaveMentionNotificationAsync(
                currentUserId,
                request.MentionedUserId,
                request.TargetId,
                mentionType.ToString());
            return Ok();
        }

        [HttpDelete("{notificationUid}")]
        public async Task<IActionResult> DeleteNotification(string notificationUid)
        {
            var uidValidationError = this.ValidateWithAttribute(
            notificationUid,
            new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
            memberName: "notificationUid",
            statusCode: 400);
            if (uidValidationError != null) return uidValidationError;

            await _notificationService.DeleteNotificationAsync(notificationUid);
            return Ok();
        }

        [HttpPut("{notificationUid}/read")]
        public async Task<IActionResult> MarkAsRead(string notificationUid)
        {
            var uidValidationError = this.ValidateWithAttribute(
            notificationUid,
            new SafeUidAttribute(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true),
            memberName: "notificationUid",
            statusCode: 400);
            if (uidValidationError != null) return uidValidationError;

            await _notificationService.MarkNotificationAsReadAsync(notificationUid);
            return Ok();
        }

        // Push token management endpoints
        [HttpPost("push-token")]
        public async Task<IActionResult> SavePushToken([FromBody] PushTokenRequest request)
        {
            // Validate request is not null
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            // Validate ExpoToken using global validation attribute
            var expoTokenValidationError = this.ValidateWithAttribute(
                request.ExpoToken,
                new SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10),
                memberName: "ExpoToken",
                statusCode: 400);
            if (expoTokenValidationError != null) return expoTokenValidationError;

            // Validate DeviceId using global validation attribute
            var deviceIdValidationError = this.ValidateWithAttribute(
                request.DeviceId,
                new SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3),
                memberName: "DeviceId",
                statusCode: 400);
            if (deviceIdValidationError != null) return deviceIdValidationError;

            var currentUserId = _currentUserService.GetUserId();
            
            // Save the push token
            await _notificationService.SavePushTokenAsync(currentUserId, request.ExpoToken, request.DeviceId);
            
            // Create notification settings for this device
            await _userService.CreateNotificationSettingsForDeviceAsync(request.DeviceId, request.ExpoToken);
            
            return Ok();
        }

        [HttpDelete("push-token/{deviceId}")]
        public async Task<IActionResult> DeletePushToken(string deviceId)
        {
            var deviceIdValidationError = this.ValidateWithAttribute(
                deviceId,
                new SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3),
                memberName: "DeviceId",
                statusCode: 400);
            if (deviceIdValidationError != null) return deviceIdValidationError;

            var currentUserId = _currentUserService.GetUserId();
            await _notificationService.DeletePushTokenAsync(currentUserId, deviceId);
            return Ok();
        }

        [HttpGet("push-tokens")]
        public async Task<IActionResult> GetPushTokens()
        {
            var currentUserUid = _currentUserService.GetUserId();
            var tokens = await _notificationService.GetUserPushTokensAsync(currentUserUid);
            return Ok(tokens);
        }

        // /// <summary>
        // /// Clean up push tokens for a specific device (admin only)
        // /// </summary>
        // [HttpPost("cleanup-device-tokens")]
        // [Authorize(Roles = "Admin")] // Only admins can manually clean up tokens
        // public async Task<IActionResult> CleanupDeviceTokens([FromBody] CleanupDeviceTokensRequest request)
        // {
        //     await _notificationService.CleanupPushTokensForLoggedOutDeviceAsync(request.UserId, request.DeviceId);
        //     return Ok(new { message = "Device tokens cleaned up successfully" });
        // }

        // /// <summary>
        // /// Clean up all stale push tokens (admin only)
        // /// </summary>
        // [HttpPost("cleanup-all-stale-tokens")]
        // [Authorize(Roles = "Admin")] // Only admins can manually clean up all tokens
        // public async Task<IActionResult> CleanupAllStaleTokens()
        // {
        //     await _notificationService.CleanupAllStalePushTokensAsync();
        //     return Ok(new { message = "All stale tokens cleaned up successfully" });
        // }

    }

    public class LikeNotificationRequest
    {
        public string PostId { get; set; }
    }

    public class CommentNotificationRequest
    {
        public string PostId { get; set; }
        public string CommentId { get; set; }
    }

    public class NewPostNotificationRequest
    {
        public string PostId { get; set; }
    }

    public class MentionNotificationRequest
    {
        [SafeUid(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true)]
        public string MentionedUserId { get; set; }
        [SafeUid(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true)]
        public string TargetId { get; set; }
        [Required(ErrorMessage = "MentionType is required")]
        public string MentionType { get; set; } // "post" or "comment"
    }

    public class PushTokenRequest
    {
        [SafeExpoTokenAttribute(allowNullValue: false, maxLength: 512, minLength: 10)]
        public string ExpoToken { get; set; }
        
        [SafeDeviceIdAttribute(allowNullValue: false, maxLength: 128, minLength: 3)]
        public string DeviceId { get; set; }
    }

    public class CleanupDeviceTokensRequest
    {
        public string UserId { get; set; }
        public string DeviceId { get; set; }
    }
} 