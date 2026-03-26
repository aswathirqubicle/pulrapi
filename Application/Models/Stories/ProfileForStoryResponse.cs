using System;
using System.Collections.Generic;

namespace Core.Application.Models.Stories
{
    public class ProfileForStoryResponse
    {
        public string FullName { get; internal set; }
        public string FirstName { get; internal set; }
        public string LastName { get; internal set; }
        public string DisplayName { get; internal set; }
        public string UserType { get; internal set; }
        public string ImageUrl { get; internal set; }
        public string Uid { get; internal set; }
        public string Username { get; internal set; }
        public bool IsInfluencer { get; internal set; }
        public bool FollowedByMe { get; internal set; }
        public string UserId { get; internal set; }
        public string StoreName { get; internal set; }
        public string StoreImageUrl { get; internal set; }
        public string StoreUid { get; internal set; }
        public string StoreUniqueName { get; internal set; }
        public bool IsStore { get; internal set; }
        public DateTime LastStoryCreatedAt { get; internal set; }
        public List<string> StoryUids { get; set; } = new List<string>();
        public int ActiveStoriesCount { get; set; }
        public int StoriesSeenCount { get; set; }
        public int UnseenStoriesCount { get; set; }
    }
}
