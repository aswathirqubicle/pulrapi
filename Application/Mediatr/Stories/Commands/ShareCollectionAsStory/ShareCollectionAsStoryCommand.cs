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
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stories.Commands.ShareCollectionAsStory;

public class ShareCollectionAsStoryCommand : IRequest<StoryWithProfileResponse>
{
    public string CollectionUid { get; set; }
    public List<string> Colors { get; set; } = new List<string>();
}

public class ShareCollectionAsStoryCommandHandler : IRequestHandler<ShareCollectionAsStoryCommand, StoryWithProfileResponse>
{
    private readonly ILogger<ShareCollectionAsStoryCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public ShareCollectionAsStoryCommandHandler(ILogger<ShareCollectionAsStoryCommandHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext, INotificationService notificationService)
    {
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<StoryWithProfileResponse> Handle(ShareCollectionAsStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var collection = await _dbContext.BookmarkCollections
                .AsSplitQuery()
                .Include(c => c.Profile).ThenInclude(p => p.User)
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                        .ThenInclude(p => p.MediaFile)
                .SingleOrDefaultAsync(c => c.Uid == request.CollectionUid, cancellationToken);

            if (collection == null)
                throw new NotFoundException("Collection not found");

            // Get the first post image from the collection for the story
            var firstPostImage = collection.BookmarkCollectionItems
                .OrderByDescending(bci => bci.CreatedAt)
                .FirstOrDefault()?.Post.MediaFile;

            if (firstPostImage == null)
                throw new BadRequestException("Collection must have at least one post with an image to share as story");

            var storyProfileMentions = new List<StoryProfileMention>()
            {
                new StoryProfileMention
                {
                    ProfileId = collection.Profile.Id
                }
            };

            var story = new Story
            {
                Text = $"Check out this collection: {collection.Name}",
                StoryExpiresIn = DateTime.UtcNow.AddHours(24),
                MediaFile = firstPostImage,
                StoryProductTags = new List<StoryProductTag>(),
                StoryProfileMentions = storyProfileMentions,
                User = currentUser,
                Store = null,
                StoryHashTags = new List<StoryHashTag>(),
                SharedCollectionId = collection.Id,
                StoryType = StoryTypeEnum.Collection,
                Colors = request.Colors
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
                            PostedByStore = false,
                            TaggedProducts = s.StoryProductTags.Select(stp =>
                                new ProductTagCoordinatesResponse
                                {
                                    PositionLeftPercent = stp.PositionLeftPercent,
                                    PositionTopPercent = stp.PositionTopPercent,
                                }),
                            CreatedAt = s.CreatedAt,
                            StoryType = s.StoryType,
                            SharedCollectionPreview = s.SharedCollection != null ? new SharedCollectionPreviewDto
                            {
                                CollectionUid = s.SharedCollection.Uid,
                                OwnerUsername = s.SharedCollection.Profile.User.UserName,
                                OwnerProfileImageUrl = s.SharedCollection.Profile.ImageUrl,
                                CollectionName = s.SharedCollection.Name,
                                TotalPostCount = s.SharedCollection.BookmarkCollectionItems.Count,
                                First4PostImageUrls = s.SharedCollection.BookmarkCollectionItems
                                    .OrderByDescending(bci => bci.CreatedAt)
                                    .Take(4)
                                    .Select(bci => bci.Post.MediaFile.Url)
                                    .ToList()
                            } : null,
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
            _logger.LogError(e, "Error sharing collection as story");
            throw;
        }
    }
}
