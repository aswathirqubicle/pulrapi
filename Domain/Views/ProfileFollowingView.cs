using Core.Domain.Entities;
using System;
using System.Collections.Generic;

namespace Core.Domain.Views
{
    public class ProfileFollowingView
    {
        public string Uid { get; set; }
        public string ImageUrl { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string About { get; set; }
        public string PhoneNumber { get; set; }
        public string Location { get; set; }
        public string Email { get; set; }
        public bool FollowedByMe { get; set; }
        public int Followers { get; set; }
        public int Following { get; set; }
        public List<Store> Stores { get; set; } = new List<Store>();

        // CurrencyDetailsResponse
        //public Currency Currency { get; set; }

        public int PostsCount { get; set; }
        public int ActiveStoriesCount { get; set; }
        // StoryResponse
        public List<Story> Stories { get; set; } = new List<Story>();
        public bool IsInfluencer { get; set; }
        public bool IsStore { get; set; }
        public string StoreName { get; set; }
        public string StoreUniqueName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
