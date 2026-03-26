using AutoMapper;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Stories;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Stories.Queries
{
    public class GetAccountStoriesQuery : IRequest<ProfileWithStoriesResponse>
    {
        [Required]
        public string EntityUid { get; set; }
        public bool IsStore { get; set; }
    }

    public class GetAccountStoriesQueryHandler : IRequestHandler<GetAccountStoriesQuery, ProfileWithStoriesResponse>
    {
        private readonly ILogger<GetAccountStoriesQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public GetAccountStoriesQueryHandler(ILogger<GetAccountStoriesQueryHandler> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService,
            UserManager<User> userManager,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<ProfileWithStoriesResponse> Handle(GetAccountStoriesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                IQueryable<Story> queryableStories = _dbContext.Stories;

                // TODO optimize query
                // TODO, add logic based on FeedType 

                var dateTimeNow = DateTime.UtcNow;

                if (!request.IsStore)
                {
                    // Privacy enforcement for account stories
                    var targetProfile = await _dbContext.Profiles
                        .Include(p => p.ProfileSettings)
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.Uid == request.EntityUid || p.User.UserName == request.EntityUid, cancellationToken);

                    if (targetProfile == null)
                    {
                        return null;
                    }

                    // Blocking enforcement: check if current user is blocked by target or vice versa
                    if (cUser?.Profile != null)
                    {
                        var isBlocked = await _dbContext.UserBlocks.AnyAsync(
                            ub => ub.IsActive && (
                                (ub.BlockerProfileId == cUser.Profile.Uid && ub.BlockedProfileId == targetProfile.Uid) ||
                                (ub.BlockerProfileId == targetProfile.Uid && ub.BlockedProfileId == cUser.Profile.Uid)
                            ),
                            cancellationToken);

                        if (isBlocked)
                        {
                            return null;
                        }
                    }

                    var isPublic = targetProfile.ProfileSettings == null || targetProfile.ProfileSettings.IsProfilePublic;
                    if (!isPublic)
                    {
                        var isOwner = cUser?.Profile?.Uid == targetProfile.Uid;
                        var isFollower = false;
                        if (!isOwner && cUser?.Profile != null)
                        {
                            isFollower = await _dbContext.ProfileFollowers.AnyAsync(
                                pf => pf.ProfileId == targetProfile.Id && pf.FollowerId == cUser.Profile.Id,
                                cancellationToken);
                        }

                        if (!isOwner && !isFollower)
                        {
                            // Hide stories for unauthorized viewers
                            return null;
                        }
                    }

                    var profileWithStoriesQuery = queryableStories
                        .Include(s => s.User).ThenInclude(u => u.Profile)
                        .Include(s => s.StorySeens)
                        .Include(s => s.StoryLikes)
                        .Include(s => s.MediaFile)
                        .Include(s => s.StoryProductTags)
                        .Include(s => s.SharedPost).ThenInclude(p => p.User).ThenInclude(u => u.Profile)
                        .Include(s => s.SharedPost).ThenInclude(p => p.MediaFile)
                        .Where(s => s.IsActive && s.User.Profile.Uid == request.EntityUid && !s.User.IsSuspended && s.User.Profile.IsActive && s.User.Stories.Where(s => s.Store == null && s.StoryExpiresIn > dateTimeNow).Any())
                        .OrderByDescending(story => story.User.Stories.Where(p => story.Store == null && story.StoryExpiresIn > dateTimeNow).Take(1).OrderByDescending(e => e.CreatedAt).Select(story => story.CreatedAt).FirstOrDefault());

                    var firstStory = await profileWithStoriesQuery.FirstOrDefaultAsync(cancellationToken);

                    ProfileWithStoriesResponse profileWithStories = null;
                    if (firstStory != null)
                    {
                        var userStories = firstStory.User.Stories
                            .Where(story => story.IsActive && story.StoryExpiresIn > dateTimeNow && story.StoreId == null)
                            .Take(10)
                            .OrderByDescending(e => e.CreatedAt)
                            .ToList();

                        var storyResponses = userStories.Select(story => new StoryResponse()
                        {
                            Uid = story.Uid,
                            EntityUid = story.User.Profile.Uid,
                            Text = story.Text,
                            LikedByMe = cUser != null && cUser.Profile != null ? story.StoryLikes.Any(l => l.LikedById == cUser.Profile.Id) : false,
                            SeenByMe = cUser != null && cUser.Profile != null ? story.StorySeens.Any(s => s.SeenById == cUser.Profile.Id) : false,
                            LikesCount = story.StoryLikes.Count,
                            MediaFile = _mapper.Map<MediaFileDetailsResponse>(story.MediaFile),
                            PostedByStore = false,
                            Colors = story.Colors,
                            TaggedProducts = story.StoryProductTags.Select(ppt =>
                                new ProductTagCoordinatesResponse
                                {
                                    PositionLeftPercent = ppt.PositionLeftPercent,
                                    PositionTopPercent = ppt.PositionTopPercent,
                                }),
                            SharedPostPreview = story.SharedPost != null ? new SharedPostPreviewDto {
                                PostUid = story.SharedPost.Uid,
                                PostOwnerUserName = story.SharedPost.User.UserName,
                                ContentPreview = story.SharedPost.Text,
                                PostOwnerImageUrl = story.SharedPost.User.Profile.ImageUrl,
                                PostImageUrl = story.SharedPost.MediaFile != null ? story.SharedPost.MediaFile.Url : null
                            } : null,
                            CreatedAt = story.CreatedAt,
                            VideoWidth = story.VideoWidth,
                            VideoHeight = story.VideoHeight
                        }).ToList();

                        var profileResponse = new ProfileForStoryResponse
                        {
                            FullName = firstStory.User.FirstName,
                            FirstName = firstStory.User.FirstName,
                            LastName = firstStory.User.LastName,
                            DisplayName = firstStory.User.DisplayName,
                            ImageUrl = firstStory.User.Profile.ImageUrl,
                            Uid = firstStory.User.Profile.Uid,
                            UserId = firstStory.User.Id,
                            Username = firstStory.User.UserName,
                            LastStoryCreatedAt = userStories.FirstOrDefault()?.CreatedAt ?? DateTime.MinValue,
                        };

                        profileWithStories = new ProfileWithStoriesResponse
                        {
                            Profile = profileResponse,
                            Stories = storyResponses
                        };
                    }


                    if (profileWithStories != null && cUser != null)
                    {
                        var myFollows = await _dbContext.ProfileFollowers
                            .Where(pf => pf.ProfileId == cUser.Profile.Id)
                            .Select(pf => pf.Profile.Uid).ToListAsync(cancellationToken);

                        profileWithStories.Profile.FollowedByMe = myFollows.Contains(cUser.Profile.Uid);
                        profileWithStories.Profile.IsInfluencer = await _userManager.IsInRoleAsync(new User() { Id = cUser.Id }, PulrRoles.Influencer);
                        profileWithStories.Profile.StoryUids = profileWithStories.Stories.Select(s => s.Uid).ToList();
                    }

                    return profileWithStories;
                }
                else if (request.IsStore)
                {
                    var storeStories = await queryableStories
                        .Include(s => s.Store)
                        .Include(s => s.StorySeens)
                        .Include(s => s.StoryLikes)
                        .Include(s => s.MediaFile)
                        .Include(s => s.StoryProductTags)
                        .Include(s => s.SharedPost).ThenInclude(p => p.User).ThenInclude(u => u.Profile)
                        .Include(s => s.SharedPost).ThenInclude(p => p.MediaFile)
                        .Where(s => s.IsActive && !s.User.IsSuspended && s.User.Profile.IsActive && s.Store.Uid == request.EntityUid && s.StoryExpiresIn > dateTimeNow)
                        .OrderByDescending(s => s.CreatedAt)
                        .ToListAsync(cancellationToken);

                    ProfileWithStoriesResponse myStoreStories = null;
                    if (storeStories.Any())
                    {
                        var firstStory = storeStories.First();
                        var storyResponses = storeStories
                            .Take(10)
                            .Select(story => new StoryResponse()
                            {
                                Uid = story.Uid,
                                EntityUid = story.Store.Uid,
                                Text = story.Text,
                                LikedByMe = cUser != null && cUser.Profile != null ? story.StoryLikes.Any(l => l.LikedById == cUser.Profile.Id) : false,
                                SeenByMe = cUser != null && cUser.Profile != null ? story.StorySeens.Any(s => s.SeenById == cUser.Profile.Id) : false,
                                LikesCount = story.StoryLikes.Count,
                                MediaFile = _mapper.Map<MediaFileDetailsResponse>(story.MediaFile),
                                PostedByStore = true,
                                TaggedProducts = story.StoryProductTags.Select(ppt =>
                                new ProductTagCoordinatesResponse
                                {
                                    PositionLeftPercent = ppt.PositionLeftPercent,
                                    PositionTopPercent = ppt.PositionTopPercent,
                                }),
                                SharedPostPreview = story.SharedPost != null ? new SharedPostPreviewDto {
                                    PostUid = story.SharedPost.Uid,
                                    PostOwnerUserName = story.SharedPost.User.UserName,
                                    ContentPreview = story.SharedPost.Text,
                                    PostOwnerImageUrl = story.SharedPost.User.Profile.ImageUrl,
                                    PostImageUrl = story.SharedPost.MediaFile != null ? story.SharedPost.MediaFile.Url : null
                                } : null,
                                CreatedAt = story.CreatedAt,
                                VideoWidth = story.VideoWidth,
                                VideoHeight = story.VideoHeight
                            }).ToList();

                        myStoreStories = new ProfileWithStoriesResponse()
                        {
                            Profile = new ProfileForStoryResponse()
                            {
                                StoreName = firstStory.Store.Name,
                                StoreImageUrl = firstStory.Store.ImageUrl,
                                StoreUid = firstStory.Store.Uid,
                                StoreUniqueName = firstStory.Store.UniqueName,
                                IsStore = true,
                                LastStoryCreatedAt = storeStories.FirstOrDefault()?.CreatedAt ?? DateTime.MinValue,
                            },
                            Stories = storyResponses
                        };
                    }

                    if (myStoreStories.Stories.Any())
                    {
                        var myStoreFollows = await _dbContext.StoreFollowers
                            .Where(sf => sf.Store.Uid == request.EntityUid)
                            .Select(sf => sf.Store.Uid).ToListAsync(cancellationToken);

                        if(cUser != null)
                        {
                            myStoreStories.Profile.FollowedByMe = myStoreFollows.Contains(cUser.Profile.Uid);
                        }
                        myStoreStories.Profile.StoryUids = myStoreStories.Stories.Select(s => s.Uid).ToList();
                    };

                    return myStoreStories;
                }

                throw new NotFoundException();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}