using System;
using Core.Application.Models.MediaFiles;

namespace Core.Application.Models.Profiles
{
    public class TaggedByDto
    {
        public string Uid { get; set; }
        public string ImageUrl { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public DateTime PostedTimeAgo { get; set; }
        public MediaFileDetailsResponse PostMediaFile { get; set; }
        public int PostLikes { get; set; }
    }
}
