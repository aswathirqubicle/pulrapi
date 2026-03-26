using System;
using System.Collections.Generic;
using Core.Application.Mappings;
using Core.Domain.Entities;
using Profile = AutoMapper.Profile;

namespace Core.Application.Models
{
    public class CommentResponse : IMapFrom<Comment>
    {
        public string Uid { get; set; }
        public string PostUid { get; set; }
        public string ProductUid { get; set; }
        public string CommentedByUid { get; set; }
        public string PostOwnerProfileId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CommentedBy { get; set; }
        public string DisplayName { get; set; }
        public string AuthorProfileImageUrl { get; set; }
        public bool LikedByMe { get; set; }
        public int LikesCount { get; set; }
        public string ParentCommentUid { get; set; }
        public int RepliesCount { get; set; }
        public int TotalParentCommentsCount { get; set; }
        public List<CommentResponse> Replies { get; set; } = new List<CommentResponse>();

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Comment, CommentResponse>()
                .ForMember(dest => dest.CommentedBy, opt => opt.MapFrom(src => src.CommentedBy.User.UserName))
                .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.CommentedBy.User.DisplayName))
                .ForMember(dest => dest.LikesCount, opt => opt.MapFrom(src => src.CommentLikes.Count))
                .ForMember(dest => dest.ParentCommentUid, opt => opt.MapFrom(src => src.ParentComment.Uid))
                .ForMember(dest => dest.RepliesCount, opt => opt.MapFrom(src => src.Replies.Count))
                .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.Replies));
        }
    }
}