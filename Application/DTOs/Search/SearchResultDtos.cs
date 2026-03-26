using AutoMapper;
using Core.Domain.Entities;
using Core.Domain.Enums;
using System;

namespace Application.DTOs.Search;

public class PostSearchResultDto
{
    public string Uid { get; set; }
    public MediaFileDto MediaFile { get; set; }  = null!;
    public string Caption { get; set; }
    public string ThumbnailUrl { get; set; }
    public int LikesCount { get; set; }
    //public Profile Profile { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public UserBasicDto Profile { get; set; } = null!;
}

public class UserSearchResultDto
{
    public string Uid { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int FollowersCount { get; set; }
}

public class TagSearchResultDto
{
    //public string Uid { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class UserBasicDto
{
    public int Uid { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class MediaFileDto
{
    public string Url { get; set; }
    public MediaFileTypeEnum FileType { get; set; }
    public string Uid { get; set; }
}