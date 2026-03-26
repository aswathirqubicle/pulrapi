using System;
using System.Collections.Generic;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Enums;
using Core.Application.Mappings;
using Core.Domain.Entities;
using AutoMapper;
using Profile = AutoMapper.Profile;
using System.Linq;

namespace Core.Application.Models.Post
{
    public class PostResponse : IMapFrom<Core.Domain.Entities.Post>
    {
        public string Uid { get; set; }
        public string ProfileUid { get; set; }
        //public string StoreUid { get; set; }
        public string Text { get; set; }
        public string ImgDescription { get; set; }
        public string ThumbnailUrl { get; set; }
        public MediaFileDetailsResponse MediaFile { get; set; }
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public int? VideoWidth { get; set; }
        public int? VideoHeight { get; set; }
        public int LikesCount { get; set; }
        public bool LikedByMe { get; set; }
        public IEnumerable<string> TaggedProductUids { get; set; }
        public DateTime CreatedAt { get; set; }
        //public bool PostedByStore { get; set; }
        //public StoreBaseResponse Store { get; set; }
        public ProfileBaseResponse Profile { get; set; }
        public int ShareCount { get; set; }
        public bool SharedByMe { get; set; }
        public int CommentsCount { get; set; }
        // public int TotalParentCommentsCount { get; set; }
        // public int RepliesCount { get; set; }
        public int BookmarksCount { get; set; }
        public bool BookmarkedByMe { get; set; }
        public bool IsMyStyle { get; set; }
        public int MyStylesCount { get; set; }
        public int ReportsCount { get; set; }
        public virtual ICollection<TaggedUserResponse> PostProfileMentions { get; set; }
        //public virtual ICollection<string> PostStoreMentions { get; set; }
        public virtual ICollection<PostProductTagResponse> PostProductTags { get; set; }
        public virtual ICollection<string> PostHashtags { get; set; }
        public PostTypeEnum PostType { get; set; }

        public void Map(Profile profile)
        {
            profile.CreateMap<Core.Domain.Entities.Post, PostResponse>()
                .ForMember(dest => dest.ReportsCount, opt => opt.MapFrom(src =>
                    src.Reports.Count(r => r.ReportType == ReportTypeEnum.Post)))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src =>
                    src.Comments.Count(c => c.IsActive)));
        }
    }
}