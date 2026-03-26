using System;
using System.Collections.Generic;
using Core.Application.Mappings;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Stories.Queries;

public class StoryDto : IMapFrom<Story>
{
    public string Uid { get; set; }
    public string EntityUid { get; set; }
    public int LikesCount { get; set; }
    public bool LikedByMe { get; set; }
    public string Text { get; set; }
    public DateTime StoryExpiresIn { get; set; }
    public MediaFileDetailsResponse MediaFile { get; set; }
    public IEnumerable<ProductDetailsResponse> TaggedProducts { get; set; }
    public bool PostedByStore { get; set; }
    public int CommentsCount { get; set; }
    public DateTime CreatedAt { get; internal set; }
    public string UserType { get; set; }
    public int? VideoWidth { get; set; }
    public int? VideoHeight { get; set; }
    //public List<ProfileResponse> LikedBy { get; set; } = new List<ProfileResponse>();
}