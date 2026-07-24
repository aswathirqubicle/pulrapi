using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Users;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IApplicationDbContext _context;
        private readonly IExpoNotificationService _expoNotificationService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IApplicationDbContext context,
            IExpoNotificationService expoNotificationService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _expoNotificationService = expoNotificationService;
            _logger = logger;
        }

        public async Task SaveLikeNotificationAsync(
            string likerUserId,
            string targetId, // postId, commentId, storyId, etc.
            EntityTypeEnum targetType,
            ActivityActionTypeEnum activityType, // e.g., LikePost, LikeComment, etc.
            string ownerUserId = null // Optional: if you already know the owner
        )
        {
            _logger.LogInformation("Processing like notification: User {LikerUserId} liked {TargetType} {TargetId}", 
                likerUserId, targetType, targetId);

            int ownerProfileId = 0;

            // Get the owner profile ID based on the entity type
            if (ownerUserId == null)
            {
                switch (targetType)
                {
                    case EntityTypeEnum.POST:
                        var post = await _context.Posts.Include(p => p.User.Profile).FirstOrDefaultAsync(p => p.Uid == targetId);
                        if (post == null || post.User.Profile == null)
                            throw new ArgumentException("Post not found");
                        ownerUserId = post.User.Id;
                        ownerProfileId = post.User.Profile.Id;
                        break;
                    case EntityTypeEnum.PRODUCT:
                        var product = await _context.Products.Include(p => p.User).ThenInclude(u => u.Profile).FirstOrDefaultAsync(p => p.Uid == targetId);
                        if (product == null || product.User?.Profile == null)
                            throw new ArgumentException("Product not found");
                        ownerUserId = product.User.Id;
                        ownerProfileId = product.User.Profile.Id;
                        break;
                    case EntityTypeEnum.COLLECTION:
                        var collection = await _context.BookmarkCollections.Include(c => c.Profile).ThenInclude(pr => pr.User).FirstOrDefaultAsync(c => c.Uid == targetId);
                        if (collection == null || collection.Profile?.User == null)
                            throw new ArgumentException("Collection not found");
                        ownerUserId = collection.Profile.User.Id;
                        ownerProfileId = collection.Profile.Id;
                        break;
                    case EntityTypeEnum.COMMENT:
                        var comment = await _context.Comments.Include(c => c.CommentedBy).ThenInclude(p => p.User).FirstOrDefaultAsync(c => c.Uid == targetId);
                        if (comment == null || comment.CommentedBy == null)
                            throw new ArgumentException("Comment not found");
                        ownerUserId = comment.CommentedBy.UserId;
                        ownerProfileId = comment.CommentedBy.Id;
                        break;
                    case EntityTypeEnum.STORY:
                        var story = await _context.Stories.Include(s => s.User.Profile).FirstOrDefaultAsync(s => s.Uid == targetId);
                        if (story == null || story.User.Profile == null)
                            throw new ArgumentException("Story not found");
                        ownerUserId = story.User.Id;
                        ownerProfileId = story.User.Profile.Id;
                        break;
                    // Add more cases as needed
                    default:
                        throw new ArgumentException("Unsupported entity type for like notification");
                }
            }
            else
            {
                ownerProfileId = await _context.Profiles.Where(p => p.UserId == ownerUserId).Select(p => p.Id).FirstOrDefaultAsync();
            }

            // Don't create notification if user likes their own entity
            if (ownerUserId == likerUserId)
            {
                _logger.LogDebug("Preventing self-notification: User {UserId} liked their own {EntityType} {TargetId}", 
                    likerUserId, targetType, targetId);
                return;
            }

            // Additional check using helper method
            if (await ShouldPreventSelfNotification(likerUserId, targetId, targetType))
            {
                _logger.LogDebug("Preventing self-notification via helper: User {UserId} liked their own {EntityType} {TargetId}", 
                    likerUserId, targetType, targetId);
                return;
            }

            // Get liker's profile ID
            var likerProfile = await _context.Profiles
                .Where(p => p.UserId == likerUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (likerProfile == 0)
                throw new ArgumentException("Liker profile not found");

            // Save activity for the liker
            var activity = new Activity
            {
                UserId = likerUserId,
                ActionType = activityType,
                TargetId = targetId,
                TargetType = targetType
            };
            _context.Activities.Add(activity);

            // Save notification history for the owner
            var targetName = targetType switch
            {
                EntityTypeEnum.POST => "post",
                EntityTypeEnum.STORY => "story",
                EntityTypeEnum.PRODUCT => "product",
                EntityTypeEnum.COMMENT => "comment",
                _ => "content"
            };
            var actorName = await _context.Profiles
                .Where(p => p.UserId == likerUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var title = "New Like";
            var body = $"liked your {targetName}.";
            var notification = new NotificationHistory
            {
                ReceiverUserId = ownerProfileId,
                ActorUserId = likerProfile,
                ActionType = NotificationActionTypeEnum.Like,
                TargetId = targetId,
                TargetType = targetType,
                IsRead = false,
                Title = title,
                Body = body
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notification if enabled
            await SendLikePushNotificationAsync(ownerUserId, likerUserId, targetId, targetType);
        }

        public async Task SaveCommentNotificationAsync(string commenterUserId, string postId, string commentId)
        {
            // Get post owner
            var post = await _context.Posts
                .Include(p => p.User.Profile)
                .FirstOrDefaultAsync(p => p.Uid == postId);

            if (post == null || post.User.Profile == null)
                throw new ArgumentException("Post not found");

            // Don't create notification if user comments on their own post
            if (post.User.Id == commenterUserId)
                return;

            // Get commenter's profile ID
            var commenterProfile = await _context.Profiles
                .Where(p => p.UserId == commenterUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (commenterProfile == 0)
                throw new ArgumentException("Commenter profile not found");

            // Get the comment text
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Uid == commentId);
            var commentText = comment?.Content ?? "";

            // Save activity for the commenter
            var activity = new Activity
            {
                UserId = commenterUserId,
                ActionType = ActivityActionTypeEnum.CommentPost,
                TargetId = commentId,
                TargetType = EntityTypeEnum.COMMENT
            };
            _context.Activities.Add(activity);

            // Get commenter name for title/body
            var commenterName = await _context.Profiles
                .Where(p => p.UserId == commenterUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var truncatedComment = commentText.Length > 50 ? commentText.Substring(0, 47) + "..." : commentText;
            var commentTitle = "New Comment";
            var commentBody = $"commented: \"{truncatedComment}\"";

            // Save notification history for the post owner
            var notification = new NotificationHistory
            {
                ReceiverUserId = post.User.Profile.Id,
                ActorUserId = commenterProfile,
                ActionType = NotificationActionTypeEnum.Comment,
                TargetId = postId,
                IsRead = false,
                CommentText = commentText,
                TargetType = EntityTypeEnum.COMMENT,
                Title = commentTitle,
                Body = commentBody
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notification if enabled
            await SendCommentPushNotificationAsync(post.User.Id, commenterUserId, postId, commentId, commentText);
        }

        public async Task SaveProductCommentNotificationAsync(string commenterUserId, string productId, string commentId)
        {
            // Get product owner
            var product = await _context.Products
                .Include(p => p.User.Profile)
                .FirstOrDefaultAsync(p => p.Uid == productId);

            if (product == null || product.User?.Profile == null)
                throw new ArgumentException("Product not found");

            // Don't create notification if user comments on their own product
            if (product.User.Id == commenterUserId)
                return;

            // Get commenter's profile ID
            var commenterProfile = await _context.Profiles
                .Where(p => p.UserId == commenterUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (commenterProfile == 0)
                throw new ArgumentException("Commenter profile not found");

            // Get the comment text
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Uid == commentId);
            var commentText = comment?.Content ?? "";

            // Save activity for the commenter
            var activity = new Activity
            {
                UserId = commenterUserId,
                ActionType = ActivityActionTypeEnum.CommentPost, // Reuse or add CommentProduct later
                TargetId = commentId,
                TargetType = EntityTypeEnum.COMMENT
            };
            _context.Activities.Add(activity);

            // Get commenter name for title/body
            var productCommenterName = await _context.Profiles
                .Where(p => p.UserId == commenterUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var truncatedProductComment = commentText.Length > 50 ? commentText.Substring(0, 47) + "..." : commentText;
            var productCommentTitle = "New Comment";
            var productCommentBody = $"commented: \"{truncatedProductComment}\"";

            // Save notification history for the product owner
            var notification = new NotificationHistory
            {
                ReceiverUserId = product.User.Profile.Id,
                ActorUserId = commenterProfile,
                ActionType = NotificationActionTypeEnum.Comment,
                TargetId = productId,
                IsRead = false,
                CommentText = commentText,
                TargetType = EntityTypeEnum.PRODUCT,
                Title = productCommentTitle,
                Body = productCommentBody
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notification if enabled
            await SendProductCommentPushNotificationAsync(product.User.Id, commenterUserId, productId, commentId, commentText);
        }

        public async Task SaveNewPostNotificationAsync(string postOwnerUserId, string postId)
        {
            // Get post owner's profile first
            var postOwnerProfile = await _context.Profiles
                .Where(p => p.UserId == postOwnerUserId)
                .Select(p => new { p.Id, p.Uid })
                .FirstOrDefaultAsync();

            if (postOwnerProfile == null)
                throw new ArgumentException("Post owner profile not found");

            // Get user's followers (exclude the owner themselves)
            var followers = await _context.ProfileFollowers
                .Where(pf => pf.Profile.Uid == postOwnerProfile.Uid && 
                            pf.Follower.IsActive &&
                            pf.Follower.UserId != postOwnerUserId) // Exclude owner from followers
                .Select(pf => new { Uid = pf.Follower.Uid, Id = pf.Follower.Id, UserId = pf.Follower.UserId })
                .ToListAsync();

            if (!followers.Any())
            {
                _logger.LogDebug("No followers found for user {UserId} to notify about new post {PostId}", postOwnerUserId, postId);
                return;
            }

            _logger.LogDebug("Notifying {FollowerCount} followers about new post {PostId} from user {UserId}", 
                followers.Count, postId, postOwnerUserId);

            // Save activity for the post owner
            var activity = new Activity
            {
                UserId = postOwnerUserId,
                ActionType = ActivityActionTypeEnum.CreatePost,
                TargetId = postId,
                TargetType = EntityTypeEnum.POST
            };
            _context.Activities.Add(activity);

            // Get post owner name for title/body
            var postOwnerName = await _context.Profiles
                .Where(p => p.UserId == postOwnerUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var newPostTitle = "New Post";
            var newPostBody = "created a new post.";

            // Save notification history for followers
            var notifications = followers.Select(follower => new NotificationHistory
            {
                ReceiverUserId = follower.Id,
                ActorUserId = postOwnerProfile.Id,
                ActionType = NotificationActionTypeEnum.NewPost,
                TargetId = postId,
                IsRead = false,
                Title = newPostTitle,
                Body = newPostBody
            }).ToList();

            _context.NotificationHistories.AddRange(notifications);
            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notifications to followers
            var followerUserIds = followers.Select(f => f.UserId).ToList();
            await SendNewPostPushNotificationsAsync(followerUserIds, postOwnerUserId, postId);
        }

        public async Task SaveNewStoryNotificationAsync(string storyOwnerUserId, string storyUid)
        {
            // Get story with all related data to determine proper TargetType and image
            var story = await _context.Stories
                .Include(s => s.SharedPost).ThenInclude(p => p.MediaFile)
                .Include(s => s.SharedProduct).ThenInclude(p => p.ProductMediaFiles).ThenInclude(pmf => pmf.MediaFile)
                .Include(s => s.SharedCollection).ThenInclude(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post).ThenInclude(p => p.MediaFile)
                .Include(s => s.MediaFile)
                .FirstOrDefaultAsync(s => s.Uid == storyUid);

            if (story == null)
                throw new ArgumentException("Story not found");

            // Get story owner's profile first
            var storyOwnerProfile = await _context.Profiles
                .Where(p => p.UserId == storyOwnerUserId)
                .Select(p => new { p.Id, p.Uid })
                .FirstOrDefaultAsync();

            if (storyOwnerProfile == null)
                throw new ArgumentException("Story owner profile not found");

            // Get user's followers (exclude the owner themselves)
            var followers = await _context.ProfileFollowers
                .Where(pf => pf.Profile.Uid == storyOwnerProfile.Uid && 
                            pf.Follower.IsActive &&
                            pf.Follower.UserId != storyOwnerUserId) // Exclude owner from followers
                .Select(pf => new { Uid = pf.Follower.Uid, Id = pf.Follower.Id, UserId = pf.Follower.UserId })
                .ToListAsync();

            if (!followers.Any())
            {
                _logger.LogDebug("No followers found for user {UserId} to notify about new story {StoryId}", storyOwnerUserId, storyUid);
                return;
            }

            _logger.LogDebug("Notifying {FollowerCount} followers about new story {StoryId} from user {UserId}", 
                followers.Count, storyUid, storyOwnerUserId);

            // Determine TargetType and TargetId based on what's being shared
            EntityTypeEnum targetType;
            string targetId;
            
            if (story.SharedPost != null)
            {
                targetType = EntityTypeEnum.POST;
                targetId = story.SharedPost.Uid;
            }
            else if (story.SharedProduct != null)
            {
                targetType = EntityTypeEnum.PRODUCT;
                targetId = story.SharedProduct.Uid;
            }
            else if (story.SharedCollection != null)
            {
                targetType = EntityTypeEnum.COLLECTION;
                targetId = story.SharedCollection.Uid;
            }
            else
            {
                targetType = EntityTypeEnum.STORY;
                targetId = story.Uid;
            }

            // Get story owner name for title/body
            var storyOwnerName = await _context.Profiles
                .Where(p => p.UserId == storyOwnerUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var newStoryTitle = "New Story";
            var newStoryBody = "created a new story.";

            // Activity
            var activity = new Activity
            {
                UserId = storyOwnerUserId,
                ActionType = ActivityActionTypeEnum.CreateStory,
                TargetId = targetId,
                TargetType = targetType
            };
            _context.Activities.Add(activity);

            // Notifications
            var notifications = followers.Select(f => new NotificationHistory
            {
                ReceiverUserId = f.Id,
                ActorUserId = storyOwnerProfile.Id,
                ActionType = NotificationActionTypeEnum.Story,
                TargetId = targetId,
                IsRead = false,
                TargetType = targetType,
                Title = newStoryTitle,
                Body = newStoryBody
            }).ToList();
            _context.NotificationHistories.AddRange(notifications);
            await _context.SaveChangesAsync(cancellationToken: default);

            // Push
            foreach (var follower in followers)
            {
                try
                {
                    var userTokens = await GetUserPushTokensWithSettingsAsync(follower.UserId, NotificationActionTypeEnum.Story);
                    if (userTokens.Any())
                    {
                        var batchCount = await GetUnreadNotificationCountAsync(follower.UserId);
                        var notification = await _context.NotificationHistories
                            .Where(n => n.ReceiverUserId == follower.Id &&
                                        n.ActorUserId == storyOwnerProfile.Id &&
                                        n.TargetId == targetId &&
                                        n.TargetType == targetType)
                            .OrderByDescending(n => n.CreatedAt)
                            .Select(n => new { n.Uid, n.TargetType })
                            .FirstOrDefaultAsync();
                        var dataWithBatch = new {
                            type = "newStory",
                            storyId = storyUid,
                            actorUserId = storyOwnerUserId,
                            batchCount,
                            notificationId = notification?.Uid,
                            targetType = targetType.ToString()
                        };
                        var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                        await _expoNotificationService.SendNotificationsAsync(expoTokens, "New Story", "shared a new story", dataWithBatch);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending new story push notification to follower {UserId}", follower.UserId);
                }
            }
        }

        public async Task SaveMentionNotificationAsync(string mentionedByUserId, string mentionedUserId, string targetId, string mentionType)
        {
            // Get profile IDs
            var mentionedByProfile = await _context.Profiles
                .Where(p => p.UserId == mentionedByUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var mentionedProfile = await _context.Profiles
                .Where(p => p.UserId == mentionedUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (mentionedByProfile == 0 || mentionedProfile == 0)
                throw new ArgumentException("Profile not found");

            // Save mention
            var mention = new Mention
            {
                MentionedUserId = mentionedProfile,
                MentionedByUserId = mentionedByProfile,
                TargetId = targetId,
                MentionType = mentionType == "Post" ? EntityTypeEnum.POST : EntityTypeEnum.COMMENT
            };
            _context.Mentions.Add(mention);

            // Save activity for the mentioner
            var activity = new Activity
            {
                UserId = mentionedByUserId,
                ActionType = mentionType == "Post" ? ActivityActionTypeEnum.MentionPost : ActivityActionTypeEnum.MentionComment,
                TargetId = targetId,
                TargetType = mentionType == "Post" ? EntityTypeEnum.POST : EntityTypeEnum.COMMENT
            };
            _context.Activities.Add(activity);

            // Get mentioned by user name for title/body
            var mentionerName = await _context.Profiles
                .Where(p => p.UserId == mentionedByUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var mentionTitle = "You were mentioned";
            var mentionBody = $"tagged you in a {mentionType.ToLower()}";

            // Save notification history for the mentioned user
            var notification = new NotificationHistory
            {
                ReceiverUserId = mentionedProfile,
                ActorUserId = mentionedByProfile,
                ActionType = NotificationActionTypeEnum.Mention,
                TargetId = targetId,
                TargetType = mentionType == "Post" ? EntityTypeEnum.POST : EntityTypeEnum.COMMENT,
                IsRead = false,
                Title = mentionTitle,
                Body = mentionBody
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notification if enabled
            await SendMentionPushNotificationAsync(mentionedUserId, mentionedByUserId, targetId, mentionType);
        }

        public async Task<int> GetUnreadNotificationCountAsync(string receiverUserId)
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.Uid == receiverUserId || p.UserId == receiverUserId);
            if (profile == null) return 0;
            return await _context.NotificationHistories.CountAsync(n => n.ReceiverUserId == profile.Id && !n.IsRead);
        }

        // Refactored push notification for all like types
        private async Task SendLikePushNotificationAsync(string receiverUserId, string likerUserId, string targetId, EntityTypeEnum targetType)
        {
            try
            {
                _logger.LogInformation("Attempting to send like push notification: Receiver={ReceiverUserId}, Liker={LikerUserId}, Target={TargetId}, Type={TargetType}", 
                    receiverUserId, likerUserId, targetId, targetType);
                
                var userTokens = await GetUserPushTokensWithSettingsAsync(receiverUserId, NotificationActionTypeEnum.Like);
                if (!userTokens.Any())
                {
                    _logger.LogWarning("No valid push tokens found for user {UserId} to send like notification", receiverUserId);
                    return;
                }
                
                _logger.LogInformation("Found {TokenCount} valid push tokens for user {UserId}", userTokens.Count, receiverUserId);
                var liker = await _context.Profiles
                    .Where(p => p.UserId == likerUserId)
                    .Select(p => p.User.UserName)
                    .FirstOrDefaultAsync();
                var title = "New Like";
                string body;
                object data;
                var batchCount = await GetUnreadNotificationCountAsync(receiverUserId);
                
                // Get the notification ID
                var receiverProfileId = await _context.Profiles
                    .Where(p => p.UserId == receiverUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                var likerProfileId = await _context.Profiles
                    .Where(p => p.UserId == likerUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                
                var notification = await _context.NotificationHistories
                    .Where(n => n.ReceiverUserId == receiverProfileId &&
                               n.ActorUserId == likerProfileId &&
                               n.TargetId == targetId &&
                               n.ActionType == NotificationActionTypeEnum.Like)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new { n.Uid, n.TargetType })
                    .FirstOrDefaultAsync();
                
                switch (targetType)
                {
                    case EntityTypeEnum.POST:
                        body = $"{liker} liked your post.";
                        var likedPostThumbnail = await _context.Posts
                            .Where(p => p.Uid == targetId)
                            .Select(p => string.IsNullOrEmpty(p.ThumbnailUrl)
                                ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null)
                                : p.ThumbnailUrl)
                            .FirstOrDefaultAsync();
                        data = new { 
                            type = "like_post", 
                            postId = targetId, 
                            actorUserId = likerUserId,
                            actorName = liker,
                            batchCount,
                            notificationId = notification?.Uid,
                            targetType = notification?.TargetType.ToString(),
                            thumbnailUrl = likedPostThumbnail
                        };
                        break;
                    case EntityTypeEnum.COMMENT:
                        body = "liked your comment.";
                        var likedCommentPostThumbnail = await _context.Comments
                            .Where(c => c.Uid == targetId)
                            .Select(c => string.IsNullOrEmpty(c.Post.ThumbnailUrl)
                                ? (c.Post.MediaFile != null ? (c.Post.MediaFile.OriginalUrl ?? c.Post.MediaFile.Url) : null)
                                : c.Post.ThumbnailUrl)
                            .FirstOrDefaultAsync();
                        data = new { 
                            type = "like_comment", 
                            commentId = targetId, 
                            actorUserId = likerUserId,
                            actorName = liker,
                            batchCount,
                            notificationId = notification?.Uid,
                            targetType = notification?.TargetType.ToString(),
                            thumbnailUrl = likedCommentPostThumbnail
                        };
                        break;
                    case EntityTypeEnum.STORY:
                        body = "liked your story.";
                        data = new { 
                            type = "like_story", 
                            storyId = targetId, 
                            actorUserId = likerUserId,
                            actorName = liker,
                            batchCount,
                            notificationId = notification?.Uid,
                            targetType = notification?.TargetType.ToString()
                        };
                        break;
                    case EntityTypeEnum.PRODUCT:
                        body = "liked your product.";
                        data = new { 
                            type = "like_product", 
                            productId = targetId, 
                            actorUserId = likerUserId,
                            actorName = liker,
                            batchCount,
                            notificationId = notification?.Uid,
                            targetType = notification?.TargetType.ToString()
                        };
                        break;
                    default:
                        body = "liked your content.";
                        data = new { 
                            type = "like", 
                            targetId = targetId, 
                            actorUserId = likerUserId,
                            actorName = liker,
                            batchCount,
                            notificationId = notification?.Uid, 
                            targetType = notification?.TargetType.ToString()
                        };
                        break;
                }
                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                _logger.LogInformation("Sending like push notification to {TokenCount} tokens for user {UserId}", expoTokens.Count, receiverUserId);
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, data);
                _logger.LogInformation("Successfully sent like push notification to user {UserId}", receiverUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send like push notification to user {UserId}. Error: {ErrorMessage}", receiverUserId, ex.Message);
                // Don't rethrow - we want to ensure database notifications are still saved even if push fails
            }
        }

        private async Task SendCommentPushNotificationAsync(string receiverUserId, string commenterUserId, string postId, string commentId, string commentText)
        {
            try
            {
                _logger.LogInformation("Attempting to send comment push notification: Receiver={ReceiverUserId}, Commenter={CommenterUserId}, Post={PostId}, Comment={CommentId}", 
                    receiverUserId, commenterUserId, postId, commentId);
                
                var userTokens = await GetUserPushTokensWithSettingsAsync(receiverUserId, NotificationActionTypeEnum.Comment);
                if (!userTokens.Any())
                {
                    _logger.LogWarning("No valid push tokens found for user {UserId} to send comment notification", receiverUserId);
                    return;
                }
                
                _logger.LogInformation("Found {TokenCount} valid push tokens for user {UserId}", userTokens.Count, receiverUserId);
                var commenter = await _context.Profiles
                    .Where(p => p.UserId == commenterUserId)
                    .Select(p => p.User.UserName)
                    .FirstOrDefaultAsync();
                var title = $"{commenter}";
                var body = $"commented on your post: \"{commentText}\"";
                var batchCount = await GetUnreadNotificationCountAsync(receiverUserId);
                
                // Get the notification ID
                var receiverProfileId = await _context.Profiles
                    .Where(p => p.UserId == receiverUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                var commenterProfileId = await _context.Profiles
                    .Where(p => p.UserId == commenterUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                
                var notification = await _context.NotificationHistories
                    .Where(n => n.ReceiverUserId == receiverProfileId &&
                               n.ActorUserId == commenterProfileId &&
                               n.TargetId == postId &&
                               n.ActionType == NotificationActionTypeEnum.Comment)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new { n.Uid, n.TargetType })
                    .FirstOrDefaultAsync();
                
                var postThumbnail = await _context.Posts
                    .Where(p => p.Uid == postId)
                    .Select(p => string.IsNullOrEmpty(p.ThumbnailUrl)
                        ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null)
                        : p.ThumbnailUrl)
                    .FirstOrDefaultAsync();
                
                var data = new { 
                    type = "comment", 
                    postId = postId, 
                    commentId = commentId, 
                    actorUserId = commenterUserId,
                    actorName = commenter,
                    commentText = commentText, 
                    batchCount,
                    notificationId = notification?.Uid,
                    targetType = notification?.TargetType.ToString(),
                    thumbnailUrl = postThumbnail
                };
                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                _logger.LogInformation("Sending comment push notification to {TokenCount} tokens for user {UserId}", expoTokens.Count, receiverUserId);
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, data);
                _logger.LogInformation("Successfully sent comment push notification to user {UserId}", receiverUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send comment push notification to user {UserId}. Error: {ErrorMessage}", receiverUserId, ex.Message);
                // Don't rethrow - we want to ensure database notifications are still saved even if push fails
            }
        }

        private async Task SendProductCommentPushNotificationAsync(string receiverUserId, string commenterUserId, string productId, string commentId, string commentText)
        {
            try
            {
                var userTokens = await GetUserPushTokensWithSettingsAsync(receiverUserId, NotificationActionTypeEnum.Comment);
                if (!userTokens.Any())
                    return;
                var commenter = await _context.Profiles
                    .Where(p => p.UserId == commenterUserId)
                    .Select(p => p.User.UserName)
                    .FirstOrDefaultAsync();
                var title = $"{commenter}";
                var body = $"commented on your product: \"{commentText}\"";
                var batchCount = await GetUnreadNotificationCountAsync(receiverUserId);
                
                // Get the notification ID
                var receiverProfileId = await _context.Profiles
                    .Where(p => p.UserId == receiverUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                var commenterProfileId = await _context.Profiles
                    .Where(p => p.UserId == commenterUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                
                var notification = await _context.NotificationHistories
                    .Where(n => n.ReceiverUserId == receiverProfileId &&
                               n.ActorUserId == commenterProfileId &&
                               n.TargetId == productId &&
                               n.ActionType == NotificationActionTypeEnum.Comment)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new { n.Uid, n.TargetType })
                    .FirstOrDefaultAsync();
                
                var data = new { 
                    type = "comment_product", 
                    productId = productId, 
                    commentId = commentId, 
                    actorUserId = commenterUserId,
                    actorName = commenter,
                    commentText = commentText, 
                    batchCount,
                    notificationId = notification?.Uid,
                    targetType = notification?.TargetType.ToString()
                };
                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending product comment push notification to user {UserId}", receiverUserId);
            }
        }

        private async Task SendNewPostPushNotificationsAsync(List<string> followerUserIds, string postOwnerUserId, string postId)
        {
            try
            {
                var postOwner = await _context.Profiles
                    .Where(p => p.UserId == postOwnerUserId)
                    .Select(p => p.User.UserName)
                    .FirstOrDefaultAsync();
                var title = "New Post";
                var body = $"{postOwner} shared a new post";
                
                // Get post owner profile ID once
                var postOwnerProfileId = await _context.Profiles
                    .Where(p => p.UserId == postOwnerUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                
                foreach (var followerUserId in followerUserIds)
                {
                    try
                    {
                        var userTokens = await GetUserPushTokensWithSettingsAsync(followerUserId, NotificationActionTypeEnum.NewPost);
                        if (userTokens.Any())
                        {
                            var batchCount = await GetUnreadNotificationCountAsync(followerUserId);
                            
                            // Get the notification ID
                            var followerProfileId = await _context.Profiles
                                .Where(p => p.UserId == followerUserId)
                                .Select(p => p.Id)
                                .FirstOrDefaultAsync();
                            
                            var notification = await _context.NotificationHistories
                                .Where(n => n.ReceiverUserId == followerProfileId &&
                                           n.ActorUserId == postOwnerProfileId &&
                                           n.TargetId == postId &&
                                           n.ActionType == NotificationActionTypeEnum.NewPost)
                                .OrderByDescending(n => n.CreatedAt)
                                .Select(n => new { n.Uid, n.TargetType })
                                .FirstOrDefaultAsync();
                            
                            var postThumbnailUrl = await _context.Posts
                                .Where(p => p.Uid == postId)
                                .Select(p => string.IsNullOrEmpty(p.ThumbnailUrl)
                                    ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null)
                                    : p.ThumbnailUrl)
                                .FirstOrDefaultAsync();
                            
                            var dataWithBatch = new { 
                                type = "newPost", 
                                postId = postId, 
                                actorUserId = postOwnerUserId,
                                actorName = postOwner,
                                batchCount,
                                notificationId = notification?.Uid,
                                targetType = notification?.TargetType.ToString(),
                                thumbnailUrl = postThumbnailUrl
                            };
                            var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                            await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, dataWithBatch);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending new post push notification to follower {UserId}", followerUserId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending new post push notifications");
            }
        }

        private async Task SendMentionPushNotificationAsync(string receiverUserId, string mentionerUserId, string targetId, string mentionType)
        {
            try
            {
                var userTokens = await GetUserPushTokensWithSettingsAsync(receiverUserId, NotificationActionTypeEnum.Mention);
                if (!userTokens.Any())
                    return;
                var mentioner = await _context.Profiles
                    .Where(p => p.UserId == mentionerUserId)
                    .Select(p => p.User.UserName)
                    .FirstOrDefaultAsync();
                var title = "You were mentioned";
                var body = $"{mentioner} mentioned you in a {mentionType.ToLower()}";
                var batchCount = await GetUnreadNotificationCountAsync(receiverUserId);
                
                // Get the notification ID
                var receiverProfileId = await _context.Profiles
                    .Where(p => p.UserId == receiverUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                var mentionerProfileId = await _context.Profiles
                    .Where(p => p.UserId == mentionerUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                
                var notification = await _context.NotificationHistories
                    .Where(n => n.ReceiverUserId == receiverProfileId &&
                               n.ActorUserId == mentionerProfileId &&
                               n.TargetId == targetId &&
                               n.ActionType == NotificationActionTypeEnum.Mention)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new { n.Uid, n.TargetType })
                    .FirstOrDefaultAsync();
                
                var mentionThumbnailUrl = mentionType == "Post"
                    ? await _context.Posts
                        .Where(p => p.Uid == targetId)
                        .Select(p => string.IsNullOrEmpty(p.ThumbnailUrl)
                            ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null)
                            : p.ThumbnailUrl)
                        .FirstOrDefaultAsync()
                    : await _context.Comments
                        .Where(c => c.Uid == targetId)
                        .Select(c => string.IsNullOrEmpty(c.Post.ThumbnailUrl)
                            ? (c.Post.MediaFile != null ? (c.Post.MediaFile.OriginalUrl ?? c.Post.MediaFile.Url) : null)
                            : c.Post.ThumbnailUrl)
                        .FirstOrDefaultAsync();
                
                var data = new { 
                    type = "mention", 
                    targetId = targetId, 
                    mentionType = mentionType, 
                    actorUserId = mentionerUserId,
                    actorName = mentioner,
                    batchCount,
                    notificationId = notification?.Uid,
                    targetType = notification?.TargetType.ToString(),
                    thumbnailUrl = mentionThumbnailUrl
                };
                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending mention push notification to user {UserId}", receiverUserId);
            }
        }

        public async Task SaveFollowNotificationAsync(string followerUserId, string followedUserId,string profileUid)
        {
            // Don't notify if user follows themselves
            if (followerUserId == followedUserId)
                return;

            // Get follower's profile ID
            var followerProfile = await _context.Profiles
                .Where(p => p.UserId == followerUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            // Get followed user's profile ID
            var followedProfile = await _context.Profiles
                .Where(p => p.UserId == followedUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (followerProfile == 0 || followedProfile == 0)
                throw new ArgumentException("Profile not found");

            // Prevent duplicate follow notifications (guards against concurrent requests)
            var existingNotification = await _context.NotificationHistories
                .FirstOrDefaultAsync(n =>
                    n.ReceiverUserId == followedProfile &&
                    n.ActorUserId == followerProfile &&
                    n.ActionType == NotificationActionTypeEnum.Follow);
            if (existingNotification != null)
                return;

            // Get follower name for title/body
            var followerName = await _context.Profiles
                .Where(p => p.UserId == followerUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var followTitle = "New Follower";
            var followBody = "started following you";

            // Save notification history for the followed user
            var notification = new NotificationHistory
            {
                ReceiverUserId = followedProfile,
                ActorUserId = followerProfile,
                ActionType = NotificationActionTypeEnum.Follow,
                TargetId = profileUid,
                IsRead = false,
                TargetType = EntityTypeEnum.PROFILE,
                Title = followTitle,
                Body = followBody
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notification if enabled
            await SendFollowPushNotificationAsync(followedUserId, followerUserId);
        }

        public async Task SaveFollowRequestNotificationAsync(string requesterProfileUid, string targetProfileUid)
        {
            // Get requester's profile ID and check if it's public or private
            var requesterProfileData = await _context.Profiles
                .Include(p => p.ProfileSettings)
                .Where(p => p.Uid == requesterProfileUid)
                .Select(p => new {
                    p.Id,
                    IsProfilePublic = p.ProfileSettings != null ? p.ProfileSettings.IsProfilePublic : true
                })
                .FirstOrDefaultAsync();

            // Get target user's profile ID
            var targetProfile = await _context.Profiles
                .Where(p => p.Uid == targetProfileUid)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (requesterProfileData == null || requesterProfileData.Id == 0 || targetProfile == 0)
                throw new ArgumentException("Profile not found");

            // Prevent duplicate follow request notifications (guards against concurrent requests)
            var existingNotification = await _context.NotificationHistories
                .FirstOrDefaultAsync(n =>
                    n.ReceiverUserId == targetProfile &&
                    n.ActorUserId == requesterProfileData.Id &&
                    n.ActionType == NotificationActionTypeEnum.FollowRequest);
            if (existingNotification != null)
                return;

            // Get requester name for title/body
            var requesterName = await _context.Profiles
                .Where(p => p.Uid == requesterProfileUid)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var followRequestTitle = "Follow Request";
            var followRequestBody = "requested to follow you";

            // Save notification history for the target user
            var notification = new NotificationHistory
            {
                ReceiverUserId = targetProfile,
                ActorUserId = requesterProfileData.Id,
                ActionType = NotificationActionTypeEnum.FollowRequest,
                TargetId = targetProfileUid,
                IsRead = false,
                TargetType = EntityTypeEnum.PROFILE,
                RequesterProfileType = requesterProfileData.IsProfilePublic ? "public" : "private",
                Title = followRequestTitle,
                Body = followRequestBody
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notification if enabled
            await SendFollowRequestPushNotificationAsync(targetProfileUid, requesterProfileUid, requesterProfileData.IsProfilePublic);
        }

        private async Task SendFollowRequestPushNotificationAsync(string targetProfileUid, string requesterProfileUid, bool isRequesterProfilePublic)
        {
            try
            {
                // Get target user's UserId for push notification
                var targetUserId = await _context.Profiles
                    .Where(p => p.Uid == targetProfileUid)
                    .Select(p => p.UserId)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(targetUserId))
                    return;

                var userTokens = await GetUserPushTokensWithSettingsAsync(targetUserId, NotificationActionTypeEnum.FollowRequest);
                if (!userTokens.Any())
                    return;

                var requester = await _context.Profiles
                    .Where(p => p.Uid == requesterProfileUid)
                    .Select(p => p.User.UserName)
                    .FirstOrDefaultAsync();

                var title = $"{requester}";
                var body = "sent you a follow request.";
                var batchCount = await GetUnreadNotificationCountAsync(targetUserId);
                
                // Get the notification ID
                var targetProfileId = await _context.Profiles
                    .Where(p => p.Uid == targetProfileUid)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                var requesterProfileId = await _context.Profiles
                    .Where(p => p.Uid == requesterProfileUid)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                
                var notification = await _context.NotificationHistories
                    .Where(n => n.ReceiverUserId == targetProfileId &&
                               n.ActorUserId == requesterProfileId &&
                               n.ActionType == NotificationActionTypeEnum.FollowRequest)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new { n.Uid, n.TargetType })
                    .FirstOrDefaultAsync();
                
                var data = new { 
                    type = "follow_request", 
                    actorUserId = requesterProfileUid,
                    actorName = requester,
                    actorProfileType = isRequesterProfilePublic ? "public" : "private", // Include requester's profile type
                    batchCount,
                    notificationId = notification?.Uid,
                    targetType = notification?.TargetType.ToString()
                };
                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending follow request push notification to user {TargetProfileUid}", targetProfileUid);
            }
        }

        private async Task SendFollowPushNotificationAsync(string receiverUserId, string followerUserId)
        {
            try
            {
                var userTokens = await GetUserPushTokensWithSettingsAsync(receiverUserId, NotificationActionTypeEnum.Follow);
                if (!userTokens.Any())
                    return;
                var follower = await _context.Profiles
                    .Where(p => p.UserId == followerUserId)
                    .Select(p => p.User.UserName)
                    .FirstOrDefaultAsync();
                var title = $"{follower}";
                var body = "started following you.";
                var batchCount = await GetUnreadNotificationCountAsync(receiverUserId);
                
                // Get the notification ID
                var receiverProfileId = await _context.Profiles
                    .Where(p => p.UserId == receiverUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                var followerProfileId = await _context.Profiles
                    .Where(p => p.UserId == followerUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                
                var notification = await _context.NotificationHistories
                    .Where(n => n.ReceiverUserId == receiverProfileId &&
                               n.ActorUserId == followerProfileId &&
                               n.ActionType == NotificationActionTypeEnum.Follow)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new { n.Uid, n.TargetType })
                    .FirstOrDefaultAsync();
                
                var data = new { 
                    type = "follow", 
                    actorUserId = followerUserId,
                    actorName = follower,
                    batchCount,
                    notificationId = notification?.Uid,
                    targetType = notification?.TargetType.ToString()
                };
                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending follow push notification to user {UserId}", receiverUserId);
            }
        }

        public async Task SaveCollectionShareNotificationAsync(string senderUserId, string receiverUserId, string collectionUid, string message)
        {
            // Don't notify if user shares with themselves
            if (senderUserId == receiverUserId)
                return;
            var follerProfileId = await _context.Profiles
                .Where(p => p.UserId == senderUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var receiverProfile = await _context.Profiles
                .Where(p => p.UserId == receiverUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            // Check if notification already exists to prevent duplicates
            var existingNotification = await _context.NotificationHistories
                .FirstOrDefaultAsync(n => n.ReceiverUserId == receiverProfile &&
                                        n.ActorUserId == follerProfileId &&
                                        n.TargetId == collectionUid &&
                                        n.ActionType == NotificationActionTypeEnum.CollectionShare &&
                                        n.CreatedAt >= DateTime.UtcNow.AddMinutes(-5)); // Within last 5 minutes

            if (existingNotification != null)
                return; // Notification already exists, don't create duplicate

            // Get sender name and collection name for title/body
            var senderName = await _context.Profiles
                .Where(p => p.UserId == senderUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var collectionName = await _context.BookmarkCollections
                .Where(c => c.Uid == collectionUid)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? "a collection";
            var collectionShareTitle = "Collection Shared";
            var collectionShareBody = $"shared a collection with you: {collectionName}";

            // Save to notification history
            var notification = new NotificationHistory
            {
                ReceiverUserId = receiverProfile,
                ActorUserId = follerProfileId,
                ActionType = NotificationActionTypeEnum.CollectionShare,
                TargetType = EntityTypeEnum.COLLECTION,
                TargetId = collectionUid,
                CommentText = message,
                IsRead = false,
                Title = collectionShareTitle,
                Body = collectionShareBody
            };
            _context.NotificationHistories.Add(notification);

            // Save to activity history
            var activity = new Activity
            {
                UserId = senderUserId,
                ActionType = ActivityActionTypeEnum.CollectionShare, // Add this to your enum if not present
                TargetId = collectionUid,
                TargetType = EntityTypeEnum.COLLECTION
            };
            _context.Activities.Add(activity);

            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notification if enabled
            await SendCollectionSharePushNotificationAsync(receiverUserId, senderUserId, collectionUid, message);
        }

        private async Task SendCollectionSharePushNotificationAsync(string receiverUserId, string senderUserId, string collectionUid, string message)
        {
            try
            {
                var userTokens = await GetUserPushTokensWithSettingsAsync(receiverUserId, NotificationActionTypeEnum.CollectionShare);
                if (!userTokens.Any())
                    return;
                var sender = await _context.Profiles
                    .Where(p => p.UserId == senderUserId)
                    .Select(p => p.User.UserName)
                    .FirstOrDefaultAsync();
                var title = $"{sender}";
                var body = $"shared a collection with you: \"{message}\"";
                var batchCount = await GetUnreadNotificationCountAsync(receiverUserId);
                
                // Get the notification ID
                var receiverProfileId = await _context.Profiles
                    .Where(p => p.UserId == receiverUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                var senderProfileId = await _context.Profiles
                    .Where(p => p.UserId == senderUserId)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync();
                
                var notification = await _context.NotificationHistories
                    .Where(n => n.ReceiverUserId == receiverProfileId &&
                               n.ActorUserId == senderProfileId &&
                               n.TargetId == collectionUid &&
                               n.ActionType == NotificationActionTypeEnum.CollectionShare)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new { n.Uid, n.TargetType })
                    .FirstOrDefaultAsync();
                
                var data = new { 
                    type = "collection_share", 
                    collectionUid = collectionUid,
                    actorUserId = senderUserId,
                    actorName = sender,
                    message = message,
                    batchCount,
                    notificationId = notification?.Uid,
                    targetType = notification?.TargetType.ToString()
                };
                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending collection share push notification to user {UserId}", receiverUserId);
            }
        }

        private async Task<List<UserPushToken>> GetUserPushTokensWithSettingsAsync(string userId, NotificationActionTypeEnum actionType)
        {
            // Validate userId is not null or empty
            if (string.IsNullOrEmpty(userId))
                return new List<UserPushToken>();

            // Get all user's push tokens (only active/valid ones)
            var userTokens = await _context.UserPushTokens
                .Where(t => t.UserId == userId && 
                           !string.IsNullOrEmpty(t.ExpoToken) &&
                           !string.IsNullOrEmpty(t.DeviceId))
                .ToListAsync();

            if (!userTokens.Any())
                return new List<UserPushToken>();

            // Get notification settings for each device
            var tokensWithSettings = new List<UserPushToken>();
            
            foreach (var token in userTokens)
            {
                // Validate token format (basic Expo token validation)
                if (!IsValidExpoToken(token.ExpoToken))
                    continue;

                // Check if user is currently logged in on this device
                // Note: This check might be too restrictive and could block legitimate notifications
                // Consider making this configurable or less strict
                var isUserLoggedInOnDevice = await IsUserCurrentlyLoggedInOnDeviceAsync(userId, token.DeviceId);
                if (!isUserLoggedInOnDevice)
                {
                    _logger.LogWarning("User {UserId} appears to be logged out on device {DeviceId}, but still has push token. This might indicate a stale token or timing issue.", userId, token.DeviceId);
                    // Instead of skipping, we'll still try to send the notification
                    // but log it for monitoring purposes
                }

                var settings = await _context.UserNotificationSettings
                    .FirstOrDefaultAsync(s => s.UserId == userId && 
                                            s.DeviceId == token.DeviceId && 
                                            s.PushToken == token.ExpoToken);

                if (settings != null)
                {
                    bool shouldSend = actionType switch
                    {
                        NotificationActionTypeEnum.Like => settings.Likes,
                        NotificationActionTypeEnum.Comment => settings.Comments,
                        NotificationActionTypeEnum.Mention => settings.Mentions,
                        NotificationActionTypeEnum.NewPost => settings.Follows,
                        NotificationActionTypeEnum.Story => settings.Follows, // Add Story support
                        NotificationActionTypeEnum.Follow => settings.Follows,
                        NotificationActionTypeEnum.FollowRequest => settings.Follows, // Use Follows setting for follow requests
                        NotificationActionTypeEnum.CollectionShare => settings.Follows, // Use Follows setting for collection shares
                        NotificationActionTypeEnum.RefundRequest => true,
                        NotificationActionTypeEnum.RefundApproved => true,
                        NotificationActionTypeEnum.RefundRejected => true,
                        NotificationActionTypeEnum.RefundDisputed => true,
                        NotificationActionTypeEnum.RefundResolved => true,
                        _ => true
                    };

                    if (shouldSend)
                    {
                        tokensWithSettings.Add(token);
                    }
                }
                else
                {
                    // If no settings found, assume user wants notifications (default behavior)
                    // But only for critical notifications like mentions and follows
                    bool isDefaultEnabled = actionType switch
                    {
                        NotificationActionTypeEnum.Mention => true,
                        NotificationActionTypeEnum.Follow => true,
                        NotificationActionTypeEnum.FollowRequest => true,
                        _ => false
                    };

                    if (isDefaultEnabled)
                    {
                        tokensWithSettings.Add(token);
                    }
                }
            }

            return tokensWithSettings;
        }

        /// <summary>
        /// Checks if a user is currently logged in on a specific device
        /// by verifying the latest login activity for that device
        /// </summary>
        /// <param name="userId">The user ID to check</param>
        /// <param name="deviceId">The device ID to check</param>
        /// <returns>True if user is currently logged in on the device, false otherwise</returns>
        private async Task<bool> IsUserCurrentlyLoggedInOnDeviceAsync(string userId, string deviceId)
        {
            try
            {
                // Get the latest login activity for this user and device
                var latestActivity = await _context.UserLoginActivities
                    .Where(a => a.UserId == userId && a.DeviceIdentifier == deviceId)
                    .OrderByDescending(a => a.Timestamp)
                    .FirstOrDefaultAsync();

                if (latestActivity == null)
                {
                    // No login activity found for this device
                    _logger.LogDebug("No login activity found for user {UserId} on device {DeviceId}", userId, deviceId);
                    return false;
                }

                // Check if the latest action is "Logged in"
                var isCurrentlyLoggedIn = latestActivity.Action == "Logged in";
                
                if (!isCurrentlyLoggedIn)
                {
                    _logger.LogDebug("User {UserId} is not currently logged in on device {DeviceId}. Last action: {Action}", 
                        userId, deviceId, latestActivity.Action);
                }

                return isCurrentlyLoggedIn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking login status for user {UserId} on device {DeviceId}", userId, deviceId);
                // In case of error, assume user is not logged in to prevent unwanted notifications
                return false;
            }
        }

        private bool IsValidExpoToken(string expoToken)
        {
            if (string.IsNullOrEmpty(expoToken))
                return false;

            // Basic Expo token validation - should start with ExponentPushToken or ExpoPushToken
            return expoToken.StartsWith("ExponentPushToken[") || 
                   expoToken.StartsWith("ExpoPushToken[") ||
                   expoToken.StartsWith("ExpoToken[");
        }

        private async Task<bool> ShouldPreventSelfNotification(string actorUserId, string targetId, EntityTypeEnum targetType)
        {
            // Prevent users from getting notifications for their own actions
            try
            {
                switch (targetType)
                {
                    case EntityTypeEnum.POST:
                        var post = await _context.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Uid == targetId);
                        return post?.User?.Id == actorUserId;
                    
                    case EntityTypeEnum.STORY:
                        var story = await _context.Stories.Include(s => s.User).FirstOrDefaultAsync(s => s.Uid == targetId);
                        return story?.User?.Id == actorUserId;
                    
                    case EntityTypeEnum.PRODUCT:
                        var product = await _context.Products.Include(p => p.User).FirstOrDefaultAsync(p => p.Uid == targetId);
                        return product?.User?.Id == actorUserId;
                    
                    case EntityTypeEnum.COMMENT:
                        var comment = await _context.Comments.Include(c => c.CommentedBy).FirstOrDefaultAsync(c => c.Uid == targetId);
                        return comment?.CommentedBy?.UserId == actorUserId;
                    
                    case EntityTypeEnum.COLLECTION:
                        var collection = await _context.BookmarkCollections.Include(c => c.Profile).FirstOrDefaultAsync(c => c.Uid == targetId);
                        return collection?.Profile?.UserId == actorUserId;
                    
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking self-notification prevention for user {UserId} and target {TargetId}", actorUserId, targetId);
                return false;
            }
        }

        public async Task DeleteNotificationAsync(string notificationId)
        {
            var notification = await _context.NotificationHistories
                .FirstOrDefaultAsync(n => n.Uid == notificationId);

            if (notification != null)
            {
                _context.NotificationHistories.Remove(notification);
                await _context.SaveChangesAsync(cancellationToken: default);
            }
        }

        public async Task MarkNotificationAsReadAsync(string notificationId)
        {
            var notification = await _context.NotificationHistories.FirstOrDefaultAsync(n => n.Uid == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync(cancellationToken: default);
            }
        }

        public async Task MarkAllNotificationsAsReadAsync(string userId)
        {
            // Get user's profile ID
            var userProfileId = await _context.Profiles
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (userProfileId == 0)
                return;

            var unreadNotifications = await _context.NotificationHistories
                .Where(n => n.ReceiverUserId == userProfileId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync(cancellationToken: default);
        }

        public async Task SavePushTokenAsync(string userId, string expoToken, string deviceId)
        {
            // Check if token already exists for this device
            var existingToken = await _context.UserPushTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == deviceId);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.ExpoToken = expoToken;
                existingToken.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new token
                var newToken = new UserPushToken
                {
                    UserId = userId,
                    ExpoToken = expoToken,
                    DeviceId = deviceId
                };
                _context.UserPushTokens.Add(newToken);
            }

            await _context.SaveChangesAsync(cancellationToken: default);
        }

        public async Task DeletePushTokenAsync(string userId, string deviceId)
        {
            var token = await _context.UserPushTokens
                .FirstOrDefaultAsync(t => t.UserId == userId && t.DeviceId == deviceId);

            if (token != null)
            {
                _context.UserPushTokens.Remove(token);
                await _context.SaveChangesAsync(cancellationToken: default);
            }
        }

        /// <summary>
        /// Cleans up push tokens for devices where users have logged out
        /// This should be called periodically or when users log out
        /// </summary>
        /// <param name="userId">The user ID to clean up tokens for</param>
        /// <param name="deviceId">The device ID to clean up tokens for</param>
        public async Task CleanupPushTokensForLoggedOutDeviceAsync(string userId, string deviceId)
        {
            try
            {
                // Check if user is currently logged out on this device
                var isCurrentlyLoggedIn = await IsUserCurrentlyLoggedInOnDeviceAsync(userId, deviceId);
                
                if (!isCurrentlyLoggedIn)
                {
                    // Remove push tokens for this device since user is logged out
                    var tokensToRemove = await _context.UserPushTokens
                        .Where(t => t.UserId == userId && t.DeviceId == deviceId)
                        .ToListAsync();

                    if (tokensToRemove.Any())
                    {
                        _logger.LogInformation("Cleaning up {TokenCount} push tokens for user {UserId} on logged out device {DeviceId}", 
                            tokensToRemove.Count, userId, deviceId);
                        
                        _context.UserPushTokens.RemoveRange(tokensToRemove);
                        await _context.SaveChangesAsync(cancellationToken: default);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up push tokens for user {UserId} on device {DeviceId}", userId, deviceId);
            }
        }

        /// <summary>
        /// Cleans up all stale push tokens for devices where users are not currently logged in
        /// This method should be called periodically (e.g., via a background job)
        /// </summary>
        public async Task CleanupAllStalePushTokensAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of stale push tokens");
                
                // Get all unique user-device combinations
                var userDeviceCombinations = await _context.UserPushTokens
                    .Select(t => new { t.UserId, t.DeviceId })
                    .Distinct()
                    .ToListAsync();

                var cleanedCount = 0;
                
                foreach (var combination in userDeviceCombinations)
                {
                    var isCurrentlyLoggedIn = await IsUserCurrentlyLoggedInOnDeviceAsync(combination.UserId, combination.DeviceId);
                    
                    if (!isCurrentlyLoggedIn)
                    {
                        // Remove all tokens for this user-device combination
                        var tokensToRemove = await _context.UserPushTokens
                            .Where(t => t.UserId == combination.UserId && t.DeviceId == combination.DeviceId)
                            .ToListAsync();

                        if (tokensToRemove.Any())
                        {
                            _context.UserPushTokens.RemoveRange(tokensToRemove);
                            cleanedCount += tokensToRemove.Count;
                        }
                    }
                }

                if (cleanedCount > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken: default);
                    _logger.LogInformation("Cleaned up {CleanedCount} stale push tokens", cleanedCount);
                }
                else
                {
                    _logger.LogInformation("No stale push tokens found to clean up");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup of stale push tokens");
            }
        }

        public async Task<List<UserPushToken>> GetUserPushTokensAsync(string userId)
        {
            return await _context.UserPushTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 20)
        {
            // Get user's profile ID
            var userProfile = await _context.Profiles
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (userProfile == 0)
                return new List<NotificationDto>();

            var notifications = await _context.NotificationHistories
                .Where(n => n.ReceiverUserId == userProfile)
                .Include(n => n.ActorProfile)
                    .ThenInclude(p => p.User)
                .Include(n => n.ReceiverProfile)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Where(n => n.ActorProfile != null && n.ActorProfile.User != null && n.ActorProfile.User.IsSuspended == false && n.ActorProfile.User.Profile.IsActive)
                .Select(n => new NotificationDto
                {
                    Uid = n.Uid,
                    ActorProfileId = n.ActorProfile.Uid,
                    ActorName = n.ActorProfile.User.UserName ,
                    ActorAvatar = n.ActorProfile.ImageUrl,
                    ActionType = n.ActionType.ToString(),
                    ReceiverUserId = n.ReceiverProfile.UserId,
                    ReceiverName = n.ReceiverProfile.User.UserName,
PostId = n.TargetId,
                    PostImageUrl = n.TargetType == EntityTypeEnum.POST
                        ? _context.Posts.Where(p => p.Uid == n.TargetId).Select(p => string.IsNullOrEmpty(p.ThumbnailUrl) ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null) : p.ThumbnailUrl).FirstOrDefault()
                        : n.TargetType == EntityTypeEnum.COMMENT && n.ActionType == NotificationActionTypeEnum.Comment
                            ? _context.Posts.Where(p => p.Uid == n.TargetId).Select(p => string.IsNullOrEmpty(p.ThumbnailUrl) ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null) : p.ThumbnailUrl).FirstOrDefault()
                            : n.TargetType == EntityTypeEnum.COMMENT && (n.ActionType == NotificationActionTypeEnum.Mention || n.ActionType == NotificationActionTypeEnum.Like)
                                ? _context.Comments.Where(c => c.Uid == n.TargetId).Select(c => string.IsNullOrEmpty(c.Post.ThumbnailUrl) ? (c.Post.MediaFile != null ? (c.Post.MediaFile.OriginalUrl ?? c.Post.MediaFile.Url) : null) : c.Post.ThumbnailUrl).FirstOrDefault()
                                : n.TargetType == EntityTypeEnum.PROFILE || n.TargetType == EntityTypeEnum.COLLAB_INVITE || n.TargetType == EntityTypeEnum.COLLAB
                                    ? n.ActorProfile.ImageUrl  // Follow, Collab notifications use actor's avatar
                                    : n.ActionType == NotificationActionTypeEnum.CollectionShare && n.TargetType == EntityTypeEnum.COLLECTION
                                        ? _context.BookmarkCollections
                                            .Where(c => c.Uid == n.TargetId)
                                            .Select(c => c.BookmarkCollectionItems
                                                .OrderByDescending(bci => bci.CreatedAt)
                                                .Select(bci => string.IsNullOrEmpty(bci.Post.ThumbnailUrl) ? (bci.Post.MediaFile != null ? (bci.Post.MediaFile.OriginalUrl ?? bci.Post.MediaFile.Url) : null) : bci.Post.ThumbnailUrl)
                                                .FirstOrDefault())
                                            .FirstOrDefault()
                                        : null,
                    StoryImageUrl = n.TargetType == EntityTypeEnum.STORY
                        ? _context.Stories.Where(s => s.Uid == n.TargetId).Select(s => s.MediaFile.Url).FirstOrDefault()
                        : null,
                    ProductImageUrl = n.TargetType == EntityTypeEnum.PRODUCT
                        ? _context.Products
                            .Where(p => p.Uid == n.TargetId)
                            .Select(p => p.ProductMediaFiles
                                .Where(pmf => pmf.MediaFile.IsActive)
                                .OrderBy(pmf => pmf.MediaFile.Priority)
                                .Select(pmf => pmf.MediaFile.Url)
                                .FirstOrDefault())
                            .FirstOrDefault()
                        : null,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    Value = n.ActionType == NotificationActionTypeEnum.Comment ? n.CommentText :
                              n.ActionType == NotificationActionTypeEnum.Follow ? "Followed" :
                              n.ActionType == NotificationActionTypeEnum.FollowRequest ? "Follow Request" :
                              n.ActionType == NotificationActionTypeEnum.FollowRequestAccepted ? "Follow Request Accepted" :
                              n.ActionType == NotificationActionTypeEnum.CollectionShare ?
                                _context.BookmarkCollections.Where(c => c.Uid == n.TargetId).Select(c => c.Name).FirstOrDefault() :
                              n.ActionType == NotificationActionTypeEnum.Collab_invite ? "Collaboration Invite" :
                              n.ActionType == NotificationActionTypeEnum.Collab_reject ? "Invitation Declined" :
                              n.ActionType == NotificationActionTypeEnum.Collab_accept ? "Invitation Accepted" :
                              n.ActionType == NotificationActionTypeEnum.Collab_review ? "Content Submitted" :
                              n.ActionType == NotificationActionTypeEnum.Collab_feedback ? "Feedback Received" :
                              n.ActionType == NotificationActionTypeEnum.Collab_approved ? "Collab Approved" :
                              null,
                    // check if the actor is followed by the receiver
                    ReceriverFollweredByActor = _context.ProfileFollowers
                        .Any(pf => pf.Profile.Uid == n.ActorProfile.Uid && pf.Follower.Uid == n.ReceiverProfile.Uid),
                    // check if receiver can follow back (actor follows receiver but receiver doesn't follow back)
                    CanFollowBack = _context.ProfileFollowers
                        .Any(pf => pf.Profile.Uid == n.ReceiverProfile.Uid && pf.Follower.Uid == n.ActorProfile.Uid) &&
                        !_context.ProfileFollowers
                        .Any(pf => pf.Profile.Uid == n.ActorProfile.Uid && pf.Follower.Uid == n.ReceiverProfile.Uid),
                    TargetType = n.TargetType,
                    RequesterProfileType = n.RequesterProfileType, // Include requester's profile type for follow request notifications
                    Title = n.Title,
                    Body = n.Body,
                    FollowerCount = n.FollowerCount

                })
                .ToListAsync();

            return notifications;
        }

        public async Task SaveFollowRequestAcceptedNotificationAsync(string accepterUserId, string requesterUserId, string profileUid)
        {
            // Don't notify if user accepts their own follow request
            if (accepterUserId == requesterUserId)
                return;

            // Get accepter's profile data (ID and profile type)
            var accepterProfileData = await _context.Profiles
                .Include(p => p.ProfileSettings)
                .Where(p => p.UserId == accepterUserId)
                .Select(p => new { 
                    p.Id, 
                    IsProfilePublic = p.ProfileSettings != null ? p.ProfileSettings.IsProfilePublic : true 
                })
                .FirstOrDefaultAsync();

            // Get requester's profile ID
            var requesterProfile = await _context.Profiles
                .Where(p => p.UserId == requesterUserId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            if (accepterProfileData == null || requesterProfile == 0)
                throw new ArgumentException("Profile not found");

            // Get accepter name for title/body
            var accepterName = await _context.Profiles
                .Where(p => p.UserId == accepterUserId)
                .Select(p => p.User.UserName)
                .FirstOrDefaultAsync() ?? "Someone";
            var followAcceptedTitle = "Follow Request Accepted";
            var followAcceptedBody = "accepted your follow request";

            // Save notification history for the requester (the one who originally sent the follow request)
            var notification = new NotificationHistory
            {
                ReceiverUserId = requesterProfile,
                ActorUserId = accepterProfileData.Id,
                ActionType = NotificationActionTypeEnum.FollowRequestAccepted,
                TargetId = profileUid,
                IsRead = false,
                TargetType = EntityTypeEnum.PROFILE,
                RequesterProfileType = accepterProfileData.IsProfilePublic ? "public" : "private",
                Title = followAcceptedTitle,
                Body = followAcceptedBody
            };
            _context.NotificationHistories.Add(notification);

            await _context.SaveChangesAsync(cancellationToken: default);

            // Send push notification if enabled
            await SendFollowRequestAcceptedPushNotificationAsync(requesterUserId, accepterUserId);
        }

        private async Task SendFollowRequestAcceptedPushNotificationAsync(string receiverUserId, string accepterUserId)
        {
            try
            {
                var userTokens = await GetUserPushTokensWithSettingsAsync(receiverUserId, NotificationActionTypeEnum.FollowRequestAccepted);
                if (!userTokens.Any())
                    return;

                var accepterProfile = await _context.Profiles
                    .Include(p => p.User)
                    .Include(p => p.ProfileSettings)
                    .FirstOrDefaultAsync(p => p.UserId == accepterUserId);

                if (accepterProfile == null)
                    return;

                var title = "Follow Request Accepted";
                var body = $"{accepterProfile.User.UserName} accepted your follow request";

                var pushData = new Dictionary<string, string>
                {
                    ["type"] = "follow_request_accepted",
                    ["accepterUserId"] = accepterUserId,
                    ["accepterProfileUid"] = accepterProfile.Uid,
                    ["accepterUsername"] = accepterProfile.User.UserName,
                    ["accepterProfileType"] = accepterProfile.ProfileSettings?.IsProfilePublic == true ? "public" : "private"
                };

                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, pushData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending follow request accepted push notification to user {UserId}", receiverUserId);
            }
        }

        public async Task SaveRefundRequestNotificationAsync(int buyerProfileId, int sellerProfileId, string orderProductAffiliateUid)
        {
            var notification = new NotificationHistory
            {
                ReceiverUserId = sellerProfileId,
                ActorUserId = buyerProfileId,
                ActionType = NotificationActionTypeEnum.RefundRequest,
                TargetId = orderProductAffiliateUid,
                IsRead = false,
                TargetType = EntityTypeEnum.PRODUCT,
                Title = "Refund Requested",
                Body = "requested a refund for an order."
            };
            _context.NotificationHistories.Add(notification);
            await _context.SaveChangesAsync(cancellationToken: default);

            await SendRefundPushNotificationAsync(buyerProfileId, sellerProfileId, orderProductAffiliateUid, NotificationActionTypeEnum.RefundRequest, "Refund Requested", "A buyer has requested a refund.");
        }

        public async Task SaveRefundApprovedNotificationAsync(int sellerProfileId, int buyerProfileId, string orderProductAffiliateUid)
        {
            var notification = new NotificationHistory
            {
                ReceiverUserId = buyerProfileId,
                ActorUserId = sellerProfileId,
                ActionType = NotificationActionTypeEnum.RefundApproved,
                TargetId = orderProductAffiliateUid,
                IsRead = false,
                TargetType = EntityTypeEnum.PRODUCT,
                Title = "Refund Approved",
                Body = "has approved your refund request."
            };
            _context.NotificationHistories.Add(notification);
            await _context.SaveChangesAsync(cancellationToken: default);

            await SendRefundPushNotificationAsync(sellerProfileId, buyerProfileId, orderProductAffiliateUid, NotificationActionTypeEnum.RefundApproved, "Refund Approved", "Your refund request has been approved.");
        }

        public async Task SaveRefundRejectedNotificationAsync(int sellerProfileId, int buyerProfileId, string orderProductAffiliateUid)
        {
            var notification = new NotificationHistory
            {
                ReceiverUserId = buyerProfileId,
                ActorUserId = sellerProfileId,
                ActionType = NotificationActionTypeEnum.RefundRejected,
                TargetId = orderProductAffiliateUid,
                IsRead = false,
                TargetType = EntityTypeEnum.PRODUCT,
                Title = "Refund Rejected",
                Body = "has rejected your refund request."
            };
            _context.NotificationHistories.Add(notification);
            await _context.SaveChangesAsync(cancellationToken: default);

            await SendRefundPushNotificationAsync(sellerProfileId, buyerProfileId, orderProductAffiliateUid, NotificationActionTypeEnum.RefundRejected, "Refund Rejected", "Your refund request has been rejected.");
        }

        public async Task SaveRefundDisputedNotificationAsync(int buyerProfileId, int? sellerProfileId, string orderProductAffiliateUid)
        {
            var notification = new NotificationHistory
            {
                ReceiverUserId = buyerProfileId,
                ActorUserId = 0,
                ActionType = NotificationActionTypeEnum.RefundDisputed,
                TargetId = orderProductAffiliateUid,
                IsRead = false,
                TargetType = EntityTypeEnum.PRODUCT,
                Title = "Refund Disputed",
                Body = "Your refund has been escalated to an admin."
            };
            _context.NotificationHistories.Add(notification);
            await _context.SaveChangesAsync(cancellationToken: default);

            await SendRefundPushNotificationAsync(0, buyerProfileId, orderProductAffiliateUid, NotificationActionTypeEnum.RefundDisputed, "Refund Disputed", "Your refund has been escalated to an admin.");

            if (sellerProfileId.HasValue)
            {
                var sellerNotification = new NotificationHistory
                {
                    ReceiverUserId = sellerProfileId.Value,
                    ActorUserId = 0,
                    ActionType = NotificationActionTypeEnum.RefundDisputed,
                    TargetId = orderProductAffiliateUid,
                    IsRead = false,
                    TargetType = EntityTypeEnum.PRODUCT,
                    Title = "Refund Disputed",
                    Body = "A buyer has escalated a refund to an admin."
                };
                _context.NotificationHistories.Add(sellerNotification);
                await _context.SaveChangesAsync(cancellationToken: default);

                await SendRefundPushNotificationAsync(0, sellerProfileId.Value, orderProductAffiliateUid, NotificationActionTypeEnum.RefundDisputed, "Refund Disputed", "A buyer has escalated a refund to an admin.");
            }
        }

        public async Task SaveRefundResolvedNotificationAsync(int adminProfileId, int buyerProfileId, int? sellerProfileId, string orderProductAffiliateUid)
        {
            var buyerNotification = new NotificationHistory
            {
                ReceiverUserId = buyerProfileId,
                ActorUserId = adminProfileId,
                ActionType = NotificationActionTypeEnum.RefundResolved,
                TargetId = orderProductAffiliateUid,
                IsRead = false,
                TargetType = EntityTypeEnum.PRODUCT,
                Title = "Refund Resolved",
                Body = "An admin has resolved the refund dispute."
            };
            _context.NotificationHistories.Add(buyerNotification);
            await _context.SaveChangesAsync(cancellationToken: default);

            await SendRefundPushNotificationAsync(adminProfileId, buyerProfileId, orderProductAffiliateUid, NotificationActionTypeEnum.RefundResolved, "Refund Resolved", "An admin has resolved the refund dispute.");

            if (sellerProfileId.HasValue)
            {
                var sellerNotification = new NotificationHistory
                {
                    ReceiverUserId = sellerProfileId.Value,
                    ActorUserId = adminProfileId,
                    ActionType = NotificationActionTypeEnum.RefundResolved,
                    TargetId = orderProductAffiliateUid,
                    IsRead = false,
                    TargetType = EntityTypeEnum.PRODUCT,
                    Title = "Refund Resolved",
                    Body = "An admin has resolved the refund dispute."
                };
                _context.NotificationHistories.Add(sellerNotification);
                await _context.SaveChangesAsync(cancellationToken: default);

                await SendRefundPushNotificationAsync(adminProfileId, sellerProfileId.Value, orderProductAffiliateUid, NotificationActionTypeEnum.RefundResolved, "Refund Resolved", "An admin has resolved the refund dispute.");
            }
        }

        private async Task SendRefundPushNotificationAsync(int actorProfileId, int receiverProfileId, string orderProductAffiliateUid, NotificationActionTypeEnum actionType, string title, string body)
        {
            try
            {
                var receiver = await _context.Profiles
                    .Where(p => p.Id == receiverProfileId)
                    .Select(p => new { p.UserId })
                    .FirstOrDefaultAsync();

                if (receiver == null || string.IsNullOrEmpty(receiver.UserId))
                    return;

                var userTokens = await GetUserPushTokensWithSettingsAsync(receiver.UserId, actionType);
                if (!userTokens.Any())
                    return;

                var batchCount = await GetUnreadNotificationCountAsync(receiver.UserId);

                var notification = await _context.NotificationHistories
                    .Where(n => n.ReceiverUserId == receiverProfileId &&
                               n.ActorUserId == actorProfileId &&
                               n.TargetId == orderProductAffiliateUid &&
                               n.ActionType == actionType)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new { n.Uid, n.TargetType })
                    .FirstOrDefaultAsync();

                var data = new
                {
                    type = actionType.ToString(),
                    orderProductAffiliateUid,
                    actorProfileId,
                    batchCount,
                    notificationId = notification?.Uid,
                    targetType = notification?.TargetType.ToString()
                };

                var expoTokens = userTokens.Select(ut => ut.ExpoToken).Distinct().ToList();
                await _expoNotificationService.SendNotificationsAsync(expoTokens, title, body, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending {ActionType} push notification to profile {ReceiverProfileId}", actionType, receiverProfileId);
            }
        }

    }
} 