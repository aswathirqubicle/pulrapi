using System;
using System.Collections.Generic;
using Core.Application.Models.Stores;

namespace Core.Application.Models.Profiles;

public class ProfileResponse
{
    public string Uid { get; set; }
    public string ImageUrl { get; set; }
    public string FullName { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserType { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
    public bool FollowedByMe { get; set; }
    public bool IsInfluencer { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
    public int PostsCount { get; set; }
    public string About { get; set; }
    public List<StoreDetailsResponse> Stores { get; set; }
    public DateTime PostedTimeAgo { get; set; }
}