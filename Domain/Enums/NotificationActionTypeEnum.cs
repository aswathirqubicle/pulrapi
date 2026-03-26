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
    }
} 