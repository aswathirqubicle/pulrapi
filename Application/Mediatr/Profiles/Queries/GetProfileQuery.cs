using Amazon;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Application.Models.Stories;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetProfileQuery : IRequest<ProfileDetailsResponse>
    {
        [SafeName(allowNullValue:false,maxLength:50,minLength:2,ErrorMessage = "Username contains invalid characters or format.")]
        public string Username { get; set; }
    }

    public class GetProfileQueryHandler(ILogger<GetProfileQueryHandler> logger,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext) : IRequestHandler<GetProfileQuery, ProfileDetailsResponse>
    {
        private readonly ILogger<GetProfileQueryHandler> _logger = logger;
        private readonly IMapper _mapper = mapper;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IApplicationDbContext _dbContext = dbContext;

        public async Task<ProfileDetailsResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                var currentProfileUid = cUser?.Profile?.Uid;
                var currentProfileId = cUser?.Profile?.Id;
                var dateTimeNow = DateTime.UtcNow;

                // ✅ STEP 1: Get all profile data + relationship flags in SINGLE QUERY
                var profileData = await _dbContext.Profiles
                    .Where(p => p.User.UserName == request.Username && p.IsActive)
                    .Select(p => new
                    {
                        // Basic Profile Info
                        Profile = new
                        {
                            p.Uid,
                            p.ImageUrl,
                            p.About,
                            p.Location,
                            p.UserId
                        },

                        // User Info
                        User = new
                        {
                            p.User.FirstName,
                            p.User.LastName,
                            p.User.DisplayName,
                            p.User.UserName,
                            p.User.Email
                        },

                        // Country
                        CountryName = p.User.Country != null ? p.User.Country.Name : null,
                        CountryUid = p.User.Country != null ? p.User.Country.Uid : null,

                        // Settings
                        UserType = p.UserType,
                        IsProfilePublic = p.ProfileSettings == null || p.ProfileSettings.IsProfilePublic,

                        // Social Media
                        SocialMedia = p.ProfileSocialMedia != null ? new
                        {
                            p.ProfileSocialMedia.WebsiteUrl,
                            p.ProfileSocialMedia.InstagramUrl,
                            p.ProfileSocialMedia.FacebookUrl,
                            p.ProfileSocialMedia.TwitterUrl,
                            p.ProfileSocialMedia.TikTokUrl
                        } : null,

                        SocialMediaLinks = p.ProfileSocialMediaLinks
                            .Select(l => new ProfileSocialMediaLinkDto
                            {
                                Url = l.Url,
                                Title = l.Title,
                                Type = l.Type
                            })
                            .ToList(),

                        // Counts
                        FollowersCount = p.ProfileFollowers.Count(),
                        FollowingCount = p.ProfileFollowings.Count(),
                        //PostsCount = p.User.Posts.Count(post => post.IsActive && post.MediaFile != null),
                        PostsCount = p.User.Posts
                            .Where(post => post.IsActive && post.MediaFile != null)
                            .SelectMany(post => post.PostLikes)
                            .Count(pl => pl.IsActive),

                        // ✅ OPTIMIZED: Efficient likes calculation
                        AllPostsLikesCount = p.User.Posts
                            .Where(post => post.IsActive && post.MediaFile != null)
                            .SelectMany(post => post.PostLikes)
                            .Count(pl => pl.IsActive),

                        // Stories
                        ActiveStoriesCount = p.User.Stories
                            .Count(s => s.IsActive && s.StoryExpiresIn > dateTimeNow),

                        StoriesSeenCount = p.User.Stories
                            .Where(s => s.IsActive && s.StoryExpiresIn > dateTimeNow)
                            .SelectMany(s => s.StorySeens)
                            .Select(seen => seen.SeenById)
                            .Distinct()
                            .Count(),

                        UnseenStoriesCount = currentProfileId.HasValue
                            ? p.User.Stories.Count(s =>
                                s.IsActive &&
                                s.StoryExpiresIn > dateTimeNow &&
                                !s.StorySeens.Any(seen => seen.SeenById == currentProfileId.Value))
                            : p.User.Stories.Count(s => s.IsActive && s.StoryExpiresIn > dateTimeNow),

                        // Stores
                        Stores = p.User.Stores
                            .Select(s => new StoreDetailsResponse
                            {
                                Uid = s.Uid,
                                UniqueName = s.UniqueName,
                                Name = s.Name,
                                ImageUrl = s.ImageUrl,
                                Followers = s.StoreFollowers.Count()
                            })
                            .ToList(),

                        // ✅ OPTIMIZED: Get relationship flags in same query (if current user exists)
                        IsBlockedByMe = currentProfileUid != null &&
                            p.BlockedUsers.Any(ub =>
                                ub.BlockerProfileId == currentProfileUid &&
                                ub.IsActive),

                        HasBlockedMe = currentProfileUid != null &&
                            p.BlockedByUsers.Any(ub =>
                                ub.BlockedProfileId == currentProfileUid &&
                                ub.IsActive),

                        IsFollowedByMe = currentProfileId.HasValue &&
                            p.ProfileFollowers.Any(pf => pf.FollowerId == currentProfileId.Value),

                        FollowsMe = currentProfileId.HasValue &&
                            p.ProfileFollowings.Any(pf => pf.ProfileId == currentProfileId.Value),

                        FollowRequestSentByMe = currentProfileUid != null &&
                            _dbContext.FollowRequests.Any(fr =>
                                fr.RequesterProfileId == currentProfileUid &&
                                fr.TargetProfileId == p.Uid &&
                                fr.IsActive),

                        FollowRequestReceivedFromTarget = currentProfileUid != null &&
                            _dbContext.FollowRequests.Any(fr =>
                                fr.RequesterProfileId == p.Uid &&
                                fr.TargetProfileId == currentProfileUid &&
                                fr.IsActive),

                        //All followers of the target profile
                        FollowedByUsernames = p.ProfileFollowers// People who follow target profile
                                .OrderByDescending(pf => pf.Follower.ProfileFollowers.Count()) // Most popular first
                                .Take(20) // Limit to 20
                                .Select(pf => pf.Follower.User.UserName)
                                .ToList(),

                        // ✅ FIXED: Get mutual followers (people who follow both)
                        MutualFollowers = currentProfileId.HasValue
                            ? _dbContext.ProfileFollowers
                                .Where(pf1 => pf1.ProfileId == p.Id) // People who follow target
                                .Where(pf1 => _dbContext.ProfileFollowers
                                    .Any(pf2 => pf2.ProfileId == currentProfileId.Value &&
                                               pf2.FollowerId == pf1.FollowerId)) // And also follow current user
                                .OrderByDescending(pf => pf.Follower.ProfileFollowers.Count())
                                .Take(10) // Limit to top 3
                                .Select(pf => pf.Follower.User.UserName)
                                .ToList()
                            : new List<string>()
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (profileData == null)
                {
                    throw new NotFoundException($"Profile with username '{request.Username}' not found.");
                }

                // ✅ STEP 2: Check if user is viewing their own profile
                bool isOwner = currentProfileUid == profileData.Profile.Uid;

                // ✅ STEP 3: Check blocking (already loaded in query) - skip if owner
                if (!isOwner && (profileData.IsBlockedByMe || profileData.HasBlockedMe))
                {
                    throw new ForbiddenException("You cannot view this profile.");
                }

                // ✅ STEP 4: Build response
                var response = new ProfileDetailsResponse
                {
                    Uid = profileData.Profile.Uid,
                    FullName = profileData.User.FirstName,
                    FirstName = profileData.User.FirstName,
                    LastName = profileData.User.LastName,
                    DisplayName = profileData.User.DisplayName,
                    Username = profileData.User.UserName,
                    Email = profileData.User.Email,
                    ImageUrl = profileData.Profile.ImageUrl,
                    About = profileData.Profile.About,
                    Location = profileData.CountryName ?? profileData.Profile.Location,
                    CountryUid = profileData.CountryUid,
                    UserType = profileData.UserType,
                    IsProfilePublic = profileData.IsProfilePublic,

                    // Social Media
                    WebsiteUrl = profileData.SocialMedia?.WebsiteUrl,
                    InstagramUrl = profileData.SocialMedia?.InstagramUrl,
                    FacebookUrl = profileData.SocialMedia?.FacebookUrl,
                    TwitterUrl = profileData.SocialMedia?.TwitterUrl,
                    TikTokUrl = profileData.SocialMedia?.TikTokUrl,
                    SocialMediaLinks = profileData.SocialMediaLinks,

                    // Counts
                    Followers = profileData.FollowersCount,
                    Following = profileData.FollowingCount,
                    PostsCount = profileData.PostsCount,
                    AllPostsLikesCount = profileData.AllPostsLikesCount,

                    // Stories
                    ActiveStoriesCount = profileData.ActiveStoriesCount,
                    StoriesSeenCount = profileData.StoriesSeenCount,
                    UnseenStoriesCount = profileData.UnseenStoriesCount,

                    // Stores
                    Stores = profileData.Stores,

                    // Relationships (if current user exists)
                    FollowedByMe = profileData.IsFollowedByMe,
                    FollowRequestSent = profileData.FollowRequestSentByMe,
                    FollowRequestReceived = profileData.FollowRequestReceivedFromTarget,
                    CanFollowBack = profileData.FollowsMe && !profileData.IsFollowedByMe,
                    FollowedBy = profileData.FollowedByUsernames,
                    MutualFollowers = profileData.MutualFollowers
                };

                // ✅ STEP 5: Handle private profile
                if (!profileData.IsProfilePublic)
                {
                    bool isFollower = profileData.IsFollowedByMe;

                    if (!isOwner && !isFollower)
                    {
                        // Return limited info for private profile
                        return new ProfileDetailsResponse
                        {
                            Uid = response.Uid,
                            FullName = response.FullName,
                            DisplayName = response.DisplayName,
                            Username = response.Username,
                            ImageUrl = response.ImageUrl,
                            About = response.About,
                            Location = response.Location,
                            UserType = response.UserType,
                            IsProfilePublic = response.IsProfilePublic,
                            Followers = response.Followers,
                            Following = response.Following,
                            PostsCount = response.PostsCount,
                            SocialMediaLinks = response.SocialMediaLinks,

                            // Relationship flags
                            FollowedByMe = response.FollowedByMe,
                            FollowRequestSent = response.FollowRequestSent,
                            FollowRequestReceived = response.FollowRequestReceived,
                            CanFollowBack = response.CanFollowBack
                        };
                    }
                }

                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving profile for username: {Username}", request.Username);
                throw;
            }
        }

        //public async Task<ProfileDetailsResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        var cUser = await _currentUserService.GetUserAsync();
        //        var dateTimeNow = DateTime.UtcNow;

        //        var profileDto = await _dbContext.Profiles
        //            .Include(p => p.User)
        //                .ThenInclude(u => u.Country)
        //            .Where(p => p.User.UserName == request.Username && p.IsActive == true)
        //            .Select(p => new ProfileDetailsResponse
        //            {
        //                Uid = p.Uid,
        //                FullName = p.User.FirstName,
        //                FirstName = p.User.FirstName,
        //                LastName = p.User.LastName,
        //                DisplayName = p.User.DisplayName,
        //                Username = p.User.UserName,
        //                UserType = p.User.Profile.UserType,
        //                IsProfilePublic = p.ProfileSettings == null ? true : p.ProfileSettings.IsProfilePublic,
        //                Email = p.User.Email,
        //                ImageUrl = p.ImageUrl,
        //                Followers = p.ProfileFollowers.Count(),
        //                Following = p.ProfileFollowings.Count(),
        //                PostsCount = p.User.Posts.Count(post => post.IsActive && post.MediaFile != null),
        //                AllPostsLikesCount = p.User.Posts.Where(post => post.IsActive && post.MediaFile != null).Sum(post => post.PostLikes.Count(pl => pl.IsActive)),
        //                About = p.About,
        //                Location = p.User.Country != null ? p.User.Country.Name : p.Location,
        //                CountryUid = p.User.Country != null ? p.User.Country.Uid : null,   
        //                WebsiteUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.WebsiteUrl : null,
        //                InstagramUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.InstagramUrl : null,
        //                FacebookUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.FacebookUrl : null,
        //                TwitterUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.TwitterUrl : null,
        //                TikTokUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.TikTokUrl : null,
        //                SocialMediaLinks = p.ProfileSocialMediaLinks != null
        //                    ? p.ProfileSocialMediaLinks.Select(l => new ProfileSocialMediaLinkDto
        //                    {
        //                        Url = l.Url,
        //                        Title = l.Title,
        //                        Type = l.Type,
        //                    }).ToList()
        //                    : new List<ProfileSocialMediaLinkDto>(),
        //                ActiveStoriesCount = p.User.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow),
        //                StoriesSeenCount = p.User.Stories
        //                    .Where(story => story.IsActive && story.StoryExpiresIn > dateTimeNow)
        //                    .SelectMany(story => story.StorySeens)
        //                    .Select(seen => seen.SeenById)
        //                    .Distinct()
        //                    .Count(),
        //                UnseenStoriesCount = cUser != null 
        //                    ? p.User.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow && !story.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id))
        //                    : p.User.Stories.Count(story => story.IsActive && story.StoryExpiresIn > dateTimeNow),
        //                //display all active stories uids in stories list
        //                //StoreUids = p.User.Stories.Where(story => story.IsActive && story.StoryExpiresIn > dateTimeNow && !story.StorySeens.Any(seen => seen.SeenById == p.Id)).Select(story => story.Uid).ToList(),

        //                Stores = p.User.Stores.Select(s => new StoreDetailsResponse
        //                {
        //                    Followers = s.StoreFollowers.Count(),
        //                    Name = s.Name,
        //                    ImageUrl = s.ImageUrl,
        //                    Uid = s.Uid,
        //                    UniqueName = s.UniqueName
        //                }).ToList()
        //            }).SingleOrDefaultAsync(cancellationToken);

        //        if (profileDto == null)
        //        {
        //            throw new NotFoundException($"Profile with username '{request.Username}' not found.");
        //        }

        //        // Block check: hide profile if blocked in either direction
        //        if (cUser != null && cUser.Profile != null)
        //        {
        //            var isBlocked = await _dbContext.UserBlocks.AnyAsync(ub =>
        //                (ub.BlockerProfileId == cUser.Profile.Uid && ub.BlockedProfileId == profileDto.Uid && ub.IsActive) ||
        //                (ub.BlockerProfileId == profileDto.Uid && ub.BlockedProfileId == cUser.Profile.Uid && ub.IsActive),
        //                cancellationToken);
        //            if (isBlocked)
        //            {
        //                throw new ForbiddenException("You cannot view this profile.");
        //            }
        //        }

        //        // Restrict access to private profiles
        //        if (!profileDto.IsProfilePublic)
        //        {
        //            bool isOwner = false;
        //            bool isFollower = false;
        //            if (cUser != null && cUser.Profile != null)
        //            {
        //                isOwner = cUser.Profile.Uid == profileDto.Uid;
        //                if (!isOwner)
        //                {
        //                    isFollower = await _dbContext.ProfileFollowers.AnyAsync(
        //                        pf => pf.Profile.Uid == profileDto.Uid && pf.Follower.Uid == cUser.Profile.Uid,
        //                        cancellationToken);
        //                }
        //            }
        //            if (!isOwner && !isFollower)
        //            {
        //                //throw new ForbiddenException("This profile is private.");
        //                //if private, return limited info Username,Profile picture,Full name,Followers count,Following count and Post count
        //                var privateProfileResponse = new ProfileDetailsResponse
        //                {
        //                    Uid = profileDto.Uid,
        //                    FullName = profileDto.FullName,
        //                    DisplayName = profileDto.DisplayName,
        //                    Username = profileDto.Username,
        //                    ImageUrl = profileDto.ImageUrl,
        //                    Followers = profileDto.Followers,
        //                    Following = profileDto.Following,
        //                    PostsCount = profileDto.PostsCount,
        //                    IsProfilePublic = profileDto.IsProfilePublic,
        //                    UserType = profileDto.UserType,
        //                    About = profileDto.About,
        //                    SocialMediaLinks = profileDto.SocialMediaLinks,
        //                    Location = profileDto.Location
        //                };

        //                // Check follow relationships and requests for private profile
        //                if (cUser != null)
        //                {
        //                    privateProfileResponse.FollowRequestSent = await _dbContext.FollowRequests
        //                        .Where(fr => fr.RequesterProfileId == cUser.Profile.Uid && 
        //                                    fr.TargetProfileId == profileDto.Uid && 
        //                                    fr.IsActive)
        //                        .AnyAsync(cancellationToken);

        //                    // Check if this private profile has sent a follow request to current user (bidirectional)
        //                    privateProfileResponse.FollowRequestReceived = await _dbContext.FollowRequests
        //                        .Where(fr => fr.RequesterProfileId == profileDto.Uid && 
        //                                    fr.TargetProfileId == cUser.Profile.Uid && 
        //                                    fr.IsActive)
        //                        .AnyAsync(cancellationToken);

        //                    // Check if current user can follow back (target follows current user but current user doesn't follow back)
        //                    var targetFollowsMe = await _dbContext.ProfileFollowers
        //                        .Include(pf => pf.Profile)
        //                        .Include(pf => pf.Follower)
        //                        .AnyAsync(pf => pf.Profile.Uid == cUser.Profile.Uid && pf.Follower.Uid == profileDto.Uid, cancellationToken);

        //                    privateProfileResponse.FollowedByMe = await _dbContext.ProfileFollowers
        //                        .Include(pf => pf.Profile)
        //                        .Include(pf => pf.Follower)
        //                        .AnyAsync(pf => pf.Follower.Uid == cUser.Profile.Uid && pf.Profile.Uid == profileDto.Uid, cancellationToken);

        //                    privateProfileResponse.CanFollowBack = targetFollowsMe && !privateProfileResponse.FollowedByMe;
        //                }

        //                return privateProfileResponse;


        //            }
        //        }

        //        if (cUser != null)
        //        {
        //            profileDto.FollowedByMe = await _dbContext.ProfileFollowers
        //                .Include(pf => pf.Profile)
        //                .Include(pf => pf.Follower)
        //                .AnyAsync(pf => pf.Follower.Uid == cUser.Profile.Uid && pf.Profile.Uid == profileDto.Uid, cancellationToken);

        //            // Check if current user has sent a follow request to this profile
        //            profileDto.FollowRequestSent = await _dbContext.FollowRequests
        //                .Where(fr => fr.RequesterProfileId == cUser.Profile.Uid && 
        //                            fr.TargetProfileId == profileDto.Uid && 
        //                            fr.IsActive)
        //                .AnyAsync(cancellationToken);

        //            // Check if this profile has sent a follow request to current user (bidirectional)
        //            profileDto.FollowRequestReceived = await _dbContext.FollowRequests
        //                .Where(fr => fr.RequesterProfileId == profileDto.Uid && 
        //                            fr.TargetProfileId == cUser.Profile.Uid && 
        //                            fr.IsActive)
        //                .AnyAsync(cancellationToken);

        //            // Check if current user can follow back (target follows current user but current user doesn't follow back)
        //            var targetFollowsMe = await _dbContext.ProfileFollowers
        //                .Include(pf => pf.Profile)
        //                .Include(pf => pf.Follower)
        //                .AnyAsync(pf => pf.Profile.Uid == cUser.Profile.Uid && pf.Follower.Uid == profileDto.Uid, cancellationToken);

        //            profileDto.CanFollowBack = targetFollowsMe && !profileDto.FollowedByMe;

        //            profileDto.FollowedBy = await _dbContext.ProfileFollowers.Where(pf => pf.FollowerId == cUser.Profile.Id || pf.Follower.Uid == profileDto.Uid)
        //                .OrderByDescending(pf => pf.Profile.ProfileFollowers.Count).Select(pf => pf.Profile.User.UserName).ToListAsync(cancellationToken);
        //        }

        //        return profileDto;
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.LogError(e, e.Message);
        //        throw;
        //    }
        //}
    }
}