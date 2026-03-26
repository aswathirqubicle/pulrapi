using Core.Domain.Enums;
using System;

namespace Application.DTOs.Search
{
    public class SearchHistoryResponseDto
    {
        public string Uid { get; set; }
        public string Term { get; set; }
        public SearchHistoryType Type { get; set; }
        public int SearchCount { get; set; }
        public DateTime UpdatedAt { get; set; }
        public ProfileDto Profile { get; set; }
        public HashtagInfoDto HashtagInfo { get; set; }
    }

    public class ProfileDto
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public int FollowersCount { get; set; }
        public string ImageUrl { get; set; }
    }

    public class HashtagInfoDto
    {
        public string Value { get; set; }
        public int Count { get; set; }
    }
} 