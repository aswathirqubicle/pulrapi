using Core.Application.Models.Users;
using Core.Domain.Entities;
using Core.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface INotificationService
    {
        //Task SaveLikeNotificationAsync(string likerUserId, string postId);
        Task SaveLikeNotificationAsync(string likerUserId, string targetId, EntityTypeEnum targetType, ActivityActionTypeEnum activityType, string ownerUserId = null);
        Task SaveCommentNotificationAsync(string commenterUserId, string postId, string commentId);
        Task SaveProductCommentNotificationAsync(string commenterUserId, string productId, string commentId);
        Task SaveNewPostNotificationAsync(string postOwnerUserId, string postId);
        Task SaveNewStoryNotificationAsync(string storyOwnerUserId, string storyUid);
        Task SaveMentionNotificationAsync(string mentionedByUserId, string mentionedUserId, string targetId, string mentionType);
        Task DeleteNotificationAsync(string notificationId);
        Task MarkNotificationAsReadAsync(string notificationId);
        Task MarkAllNotificationsAsReadAsync(string userId);
        Task<List<NotificationDto>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 20);
        
        // Push token management
        Task SavePushTokenAsync(string userId, string expoToken, string deviceId);
        Task DeletePushTokenAsync(string userId, string deviceId);
        Task<List<UserPushToken>> GetUserPushTokensAsync(string userId);
        
        // Push token cleanup methods
        Task CleanupPushTokensForLoggedOutDeviceAsync(string userId, string deviceId);
        Task CleanupAllStalePushTokensAsync();

        Task SaveFollowNotificationAsync(string followerUserId, string followedUserId,string profileUid);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task SaveFollowRequestNotificationAsync(string requesterProfileUid, string targetProfileUid);
        Task SaveFollowRequestAcceptedNotificationAsync(string accepterUserId, string requesterUserId, string profileUid);
        Task SaveCollectionShareNotificationAsync(string senderUserId, string receiverUserId, string collectionUid, string message);
    }
} 