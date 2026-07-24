using System;

namespace Core.Domain.Enums
{
    public enum NotificationActionTypeEnum
    {
        Like = 1,
        Mention = 2,
        Comment = 3,
        NewPost = 4,
        Follow = 5,
        CollectionShare = 6,
        Story = 7,
        FollowRequest = 8,
        FollowRequestAccepted = 9,
        Collab_invite = 10,
        Collab_reject = 11,
        Collab_accept = 12,
        Collab_review = 13,
        Collab_feedback = 14,
        Collab_approved = 15,
        RefundRequest = 16,
        RefundApproved = 17,
        RefundRejected = 18,
        RefundDisputed = 19,
        RefundResolved = 20,
    }
} 