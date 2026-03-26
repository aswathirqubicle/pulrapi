using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Stories;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stories.Commands.SharePostAsStory;

public class SharePostAsStoryCommand : IRequest<StoryWithProfileResponse>
{
    public string PostUid { get; set; }
    public List<string> Colors { get; set; } = new List<string>();
}

public class SharePostAsStoryCommandHandler : IRequestHandler<SharePostAsStoryCommand, StoryWithProfileResponse>
{
    private readonly ILogger<SharePostAsStoryCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public SharePostAsStoryCommandHandler(ILogger<SharePostAsStoryCommandHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext, INotificationService notificationService)
    {
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<StoryWithProfileResponse> Handle(SharePostAsStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var post = await _dbContext.Posts
                .AsSplitQuery()
                .Include(p => p.User).ThenInclude(u => u.Profile)
                .Include(p => p.Store)
                .Include(p => p.MediaFile)
                .Include(p => p.PostProductTags)
                .Include(p => p.PostProfileMentions)
                .SingleOrDefaultAsync(p => p.IsActive && !p.User.IsSuspended && p.Uid == request.PostUid, cancellationToken);

            if (post == null)
                throw new NotFoundException("Post not found");

            var storyProductTags = new List<StoryProductTag>();
            if (post.PostProductTags.Any())
            {
                storyProductTags = post.PostProductTags.Select(ppt => new StoryProductTag
                {
                    ProductId = ppt.ProductId,
                    PositionTopPercent = ppt.PositionTopPercent,
                    PositionLeftPercent = ppt.PositionLeftPercent
                }).ToList();
            }

            var storyProfileMentions = new List<StoryProfileMention>()
            {
                new StoryProfileMention
                {
                    ProfileId = post.User.Profile.Id
                    //ProfileId = currentUser.Profile.Id
                }
            };
            if (post.PostProfileMentions.Any())
            {
                storyProfileMentions.AddRange(post.PostProfileMentions.Select(ppm => new StoryProfileMention { ProfileId = ppm.ProfileId }));
            }

            var storyHashTags = new List<StoryHashTag>();

            if (post.PostHashtags.Any())
                storyHashTags.AddRange(post.PostHashtags.Select(ppm => new StoryHashTag { HashTagId = ppm.HashtagId }));

            var story = new Story
            {
                Text = post.Text,
                StoryExpiresIn = DateTime.UtcNow.AddHours(24),
                MediaFile = post.MediaFile,
                StoryProductTags = storyProductTags,
                StoryProfileMentions = storyProfileMentions,
                User = currentUser,
                Store = null,
                StoryHashTags = storyHashTags,
                SharedPostId = post.Id,
                Colors = request.Colors,
                VideoWidth = post.VideoWidth,
                VideoHeight = post.VideoHeight
            };

            _dbContext.Stories.Add(story);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = await _dbContext.Stories.Where(s => s.Uid == story.Uid)
                .Select(s =>
                    new StoryWithProfileResponse()
                    {
                        Profile = new ProfileForStoryResponse()
                        {
                            FullName = s.User.FirstName,
                            FirstName = s.User.FirstName,
                            LastName = s.User.LastName,
                            DisplayName = s.User.DisplayName,
                            ImageUrl = s.User.Profile.ImageUrl,
                            Uid = currentUser.Profile.Uid,
                            UserId = s.User.Id,
                            Username = s.User.UserName,
                            LastStoryCreatedAt = story.CreatedAt,
                        },
                        Story = new StoryResponse
                        {
                            Uid = s.Uid,
                            EntityUid = s.Uid,
                            Text = s.Text,
                            DisplayName = s.User.DisplayName,
                            LikedByMe = currentUser != null && currentUser.Profile != null && s.StoryLikes.Any(l => l.Id == currentUser.Profile.Id),
                            LikesCount = s.StoryLikes.Count,
                            MediaFile = _mapper.Map<MediaFileDetailsResponse>(s.MediaFile),
                            PostedByStore = true,
                            TaggedProducts = s.StoryProductTags.Select(stp =>
                                new ProductTagCoordinatesResponse
                                {
                                    PositionLeftPercent = stp.PositionLeftPercent,
                                    PositionTopPercent = stp.PositionTopPercent,
                                    //Product = new ProductDetailsResponse
                                    //{
                                    //    AffiliateId = stp.Product.OrderProductAffiliate.Affiliate.AffiliateId,
                                    //    ArticleCode = stp.Product.ArticleCode,
                                    //    Description = stp.Product.Description,
                                    //    Name = stp.Product.Name,
                                    //    Price = stp.Product.Price,
                                    //    Uid = stp.Product.Uid,
                                    //    PositionLeftPercent = stp.PositionLeftPercent,
                                    //    PositionTopPercent = stp.PositionTopPercent,
                                    //    ProductMediaFiles = stp.Product.ProductMediaFiles.Select(pmf =>
                                    //        new MediaFileDetailsResponse
                                    //        {
                                    //            Uid = pmf.MediaFile.Uid,
                                    //            FileType = pmf.MediaFile.MediaFileType.ToString(),
                                    //            Url = pmf.MediaFile.Url,
                                    //            Priority = pmf.MediaFile.Priority
                                    //        })
                                    //}
                                }),
                            CreatedAt = s.CreatedAt,
                            Colors = s.Colors,
                            VideoWidth = s.VideoWidth,
                            VideoHeight = s.VideoHeight
                        }
                    }
                ).SingleOrDefaultAsync(cancellationToken);

            return response;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error sharing post as story");
            throw;
        }
    }
}