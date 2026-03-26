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

namespace Core.Application.Mediatr.Stories.Commands.ShareProductAsStory;

public class ShareProductAsStoryCommand : IRequest<StoryWithProfileResponse>
{
    public string ProductUid { get; set; }
    public List<string> Colors { get; set; } = [];
}

public class ShareProductAsStoryCommandHandler : IRequestHandler<ShareProductAsStoryCommand, StoryWithProfileResponse>
{
    private readonly ILogger<ShareProductAsStoryCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public ShareProductAsStoryCommandHandler(ILogger<ShareProductAsStoryCommandHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext, INotificationService notificationService)
    {
        _logger = logger;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<StoryWithProfileResponse> Handle(ShareProductAsStoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetUserAsync();
            var product = await _dbContext.Products
                .AsSplitQuery()
                .Include(p => p.User).ThenInclude(u => u.Profile)
                .Include(p => p.Store)
                .Include(p => p.ProductMediaFiles).ThenInclude(pmf => pmf.MediaFile)
                .SingleOrDefaultAsync(p => p.IsActive && !p.User.IsSuspended && p.Uid == request.ProductUid, cancellationToken);

            if (product == null)
                throw new NotFoundException("Product not found");

            // Get the first product image for the story
            var productImage = product.ProductMediaFiles
                .OrderBy(pmf => pmf.MediaFile.Priority)
                .FirstOrDefault()?.MediaFile;

            if (productImage == null)
                throw new BadRequestException("Product must have at least one image to share as story");

            var storyProfileMentions = new List<StoryProfileMention>()
            {
                new StoryProfileMention
                {
                    ProfileId = product.User.Profile.Id
                }
            };

            var story = new Story
            {
                Text = $"Check out this {product.Name}",
                StoryExpiresIn = DateTime.UtcNow.AddHours(24),
                MediaFile = productImage,
                StoryProductTags = new List<StoryProductTag>(),
                StoryProfileMentions = storyProfileMentions,
                User = currentUser,
                Store = product.Store,
                StoryHashTags = [],
                SharedProductId = product.Id,
                StoryType = StoryTypeEnum.Product,
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
                            PostedByStore = s.Store != null,
                            TaggedProducts = s.StoryProductTags.Select(stp =>
                                new ProductTagCoordinatesResponse
                                {
                                    PositionLeftPercent = stp.PositionLeftPercent,
                                    PositionTopPercent = stp.PositionTopPercent,
                                }),
                            CreatedAt = s.CreatedAt,
                            StoryType = s.StoryType,
                            SharedProductPreview = s.SharedProduct != null ? new SharedProductPreviewDto
                            {
                                ProductUid = s.SharedProduct.Uid,
                                ProductName = s.SharedProduct.Name,
                                OwnerUsername = s.SharedProduct.User.UserName,
                                OwnerFullName = s.SharedProduct.User.FirstName,
                                OwnerProfileImageUrl = s.SharedProduct.User.Profile.ImageUrl,
                                WhatIsIt = s.SharedProduct.WhatIsIt,
                                ProductDetail = s.SharedProduct.ProductDetail,
                                MinPrice = s.SharedProduct.MinPrice,
                                MaxPrice = s.SharedProduct.MaxPrice,
                                CountryCode = s.SharedProduct.Country != null ? s.SharedProduct.Country.Iso2 : null,
                                CurrencyCode = s.SharedProduct.Country != null ? s.SharedProduct.Country.Iso4 : null,
                                //ProductImageUrl = s.SharedProduct.ProductMediaFiles
                                //    .OrderBy(pmf => pmf.MediaFile.Priority)
                                //    .FirstOrDefault()?.MediaFile.Url,
                                ImageUrls = s.SharedProduct.ProductMediaFiles.Select(pmf => pmf.MediaFile.Url).ToList(),
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
            _logger.LogError(e, "Error sharing product as story");
            throw;
        }
    }
}
