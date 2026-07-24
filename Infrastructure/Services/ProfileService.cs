using Amazon;
using AutoMapper;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Currencies;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Profile = Core.Domain.Entities.Profile;

namespace Core.Infrastructure.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ILogger<ProfileService> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public ProfileService(ILogger<ProfileService> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IQueryHelperService queryHelperService,
            IFileUploadService fileCloudService,
            INotificationService notificationService,
            UserManager<User> userManager,
            IConfiguration configuration,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _queryHelperService = queryHelperService;
            _fileUploadService = fileCloudService;
            _notificationService = notificationService;
            _userManager = userManager;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task Create(User user, GenderEnum? gender = null, string userType = null)
        {
            try
            {
                bool alreadyExists = await _dbContext.Profiles.AnyAsync(p => p.UserId == user.Id);
                if (alreadyExists)
                {
                    throw new ForbiddenException("Profile already exists");
                }

                var genderEntity = gender == null
                    ? await _dbContext.Genders.SingleOrDefaultAsync(g => g.Key == GenderEnum.Other.ToString())
                    : await _dbContext.Genders.SingleOrDefaultAsync(g => g.Key == gender.ToString());

                var profile = new Profile()
                {
                    User = user,
                    Gender = genderEntity,
                    Currency = await _dbContext.Currencies.SingleOrDefaultAsync(c =>
                        c.Code == _configuration["ProfileSettings:DefaultCurrencyCode"]),
                    ProfileSettings = new ProfileSettings
                    {
                        IsProfilePublic = true,
                        ShowSocialMediaLinks = true,
                        ShowFollowers = true,
                        ShowFollowing = true,
                        ShowLocation = true,
                        ShowAbout = true
                    },
                    UserType = userType
                };

                _dbContext.Profiles.Add(profile);
                await _dbContext.SaveChangesAsync(CancellationToken.None);

                // Create default "Saved" collection for the new user
                var defaultCollection = new BookmarkCollection
                {
                    Name = "Saved",
                    ProfileId = profile.Id,
                    ProfileUid = profile.Uid
                };

                _dbContext.BookmarkCollections.Add(defaultCollection);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        //public async Task<MyProfileDetailsResponse> GetMy()
        //{
        //    try
        //    {
        //        var user = await _currentUserService.GetUserAsync();

        //        if (user == null || user.Profile == null || !user.Profile.IsActive)
        //        {
        //            throw new ForbiddenException();
        //        }

        //        var profileMapped = _mapper.Map<MyProfileDetailsResponse>(user.Profile);
        //        profileMapped.FullName = user.FirstName;
        //        profileMapped.FirstName = user.FirstName;
        //        profileMapped.LastName = user.LastName;
        //        profileMapped.Username = user.UserName;
        //        profileMapped.Email = user.Email;
        //        profileMapped.PhoneNumber = user.PhoneNumber;

        //        // Location information is always visible
        //        profileMapped.Address = user.Address;
        //        profileMapped.ZipCode = user.ZipCode;
        //        profileMapped.CityName = user.CityName;
        //        profileMapped.Gender = user.Profile.Gender?.Key;
        //        profileMapped.Location = user.Country != null ? user.Country.Name : user.Profile.Location;
        //        profileMapped.CountryUid = user.Country != null ? user.Country.Uid : null;

        //        profileMapped.UserType = user.Profile.UserType;

        //        profileMapped.IsProfilePublic= user.Profile?.ProfileSettings?.IsProfilePublic;

        //        // Social media links are always visible
        //        profileMapped.WebsiteUrl = user.Profile.ProfileSocialMedia?.WebsiteUrl;
        //        profileMapped.InstagramUrl = user.Profile.ProfileSocialMedia?.InstagramUrl;
        //        profileMapped.FacebookUrl = user.Profile.ProfileSocialMedia?.FacebookUrl;
        //        profileMapped.TwitterUrl = user.Profile.ProfileSocialMedia?.TwitterUrl;
        //        profileMapped.TikTokUrl = user.Profile.ProfileSocialMedia?.TikTokUrl;
        //        profileMapped.SocialMediaLinks = user.Profile.ProfileSocialMediaLinks
        //            .Select(l => new ProfileSocialMediaLinkDto
        //            {
        //                Url = l.Url,
        //                Title = l.Title,
        //                Type = l.Type,
        //            }).ToList();
        //        // profileMapped.Followers = await _dbContext.ProfileFollowers
        //        //     .Where(e => e.ProfileId == user.Profile.Id)
        //        //     .Select(f => f.Follower.Uid).ToListAsync();

        //        // About section is always visible
        //        profileMapped.About = user.Profile.About;

        //        // Followers and following counts are always visible
        //        profileMapped.Followers = await _dbContext.ProfileFollowers
        //            .CountAsync(e => e.ProfileId == user.Profile.Id);
        //        profileMapped.Following = await _dbContext.ProfileFollowers
        //            .CountAsync(e => e.FollowerId == user.Profile.Id);
        //        profileMapped.PostsCount = await _dbContext.Posts
        //                .CountAsync(p => p.User.Id == user.Id && p.IsActive && p.MediaFile != null);
        //        //var AllPostsLikesCount = user.Posts.Where(p => p.IsActive && p.MediaFile != null).Sum(post => post.PostLikes.Count(pl => pl.IsActive));
        //        profileMapped.AllPostsLikesCount =  _dbContext.Posts
        //                .Where(p => p.User.Id == user.Id && p.IsActive && p.MediaFile != null).Sum(p => p.PostLikes.Count(pl => pl.IsActive));

        //        // Add IsActive check for bookmarks, comments, stories, etc. if you return them in this method or other 'my data' methods.
        //        //if (user.Profile.ProfileSettings.IsProfilePublic || user.Profile.ProfileSettings.IsProfilePublic == null)
        //        //{
        //        //    profileMapped.PostsCount = await _dbContext.Posts
        //        //        .CountAsync(p => p.User.Id == user.Id);
        //        //}
        //        //else
        //        //{
        //        //    // Check if the current user is following this profile
        //        //    var isFollowing = await _dbContext.ProfileFollowers
        //        //        .AnyAsync(pf => pf.ProfileId == user.Profile.Id && 
        //        //                      pf.Follower.UserId == _currentUserService.GetUserId());

        //        //    if (isFollowing)
        //        //    {
        //        //        profileMapped.PostsCount = await _dbContext.Posts
        //        //            .CountAsync(p => p.User.Id == user.Id);
        //        //    }
        //        //    else
        //        //    {
        //        //        profileMapped.PostsCount = 0; // Hide posts count for non-followers
        //        //    }
        //        //}

        //        if (user.Country != null)
        //        {
        //            profileMapped.CountryUid = user.Country.Uid;
        //        }

        //        if (user.Profile.CurrencyId != null)
        //        {
        //            profileMapped.Currency = _mapper.Map<CurrencyDetailsResponse>(
        //                await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Id == user.Profile.CurrencyId));
        //            if (profileMapped.Currency != null)
        //            {
        //                profileMapped.CurrencyUid = profileMapped.Currency.Uid;
        //            }
        //        }

        //        // profileMapped.Following = await _dbContext.ProfileFollowers
        //        //     .Where(e => e.FollowerId == user.Profile.Id)
        //        //     .Select(f => f.Profile.Uid).ToListAsync();

        //        profileMapped.StoreUids = await _dbContext.Stores.Where(s => s.UserId == user.Id).Select(s => s.Uid).ToListAsync();

        //        profileMapped.Stores = await _dbContext.Stores.Where(s => s.UserId == user.Id)
        //            .Select(s => new StoreDetailsResponse()
        //            {
        //                Uid = s.Uid,
        //                UniqueName = s.UniqueName,
        //                Name = s.Name,
        //                ImageUrl = s.ImageUrl
        //            }).ToListAsync();

        //        return profileMapped;
        //    }
        //    catch (Exception e)
        //    {
        //        if (e is Core.Application.Exceptions.BadRequestException || e is Core.Application.Exceptions.NotFoundException)
        //        {
        //            throw; // This preserves the original 400/404 status code
        //        }

        //        // Log and wrap other exceptions
        //        throw new Exception($"{e.Message}", e);
        //    }
        //}

        public async Task<MyProfileDetailsResponse> GetMy()
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();

                if (currentUserId == null)
                {
                    throw new ForbiddenException();
                }

                // ✅ SINGLE QUERY - Get everything at once!
                var profileData = await _dbContext.Users
                    .Where(u => u.Id == currentUserId && u.Profile != null && u.Profile.IsActive)
                    .Select(u => new
                    {
                        // User Info
                        User = new
                        {
                            u.Id,
                            u.FirstName,
                            u.LastName,
                            u.UserName,
                            u.Email,
                            u.PhoneNumber,
                            u.Address,
                            u.ZipCode,
                            u.CityName
                        },

                        // Profile Info
                        Profile = new
                        {
                            u.Profile.Uid,
                            u.Profile.ImageUrl,
                            u.Profile.About,
                            u.Profile.Location,
                            u.Profile.UserType,
                            GenderKey = u.Profile.Gender != null ? u.Profile.Gender.Key : null,
                            u.Profile.CurrencyId
                        },

                        // Country
                        Country = u.Country != null ? new
                        {
                            u.Country.Uid,
                            u.Country.Name
                        } : null,

                        // Profile Settings
                        IsProfilePublic = u.Profile.ProfileSettings != null
                            ? u.Profile.ProfileSettings.IsProfilePublic
                            : (bool?)null,

                        // Social Media
                        SocialMedia = u.Profile.ProfileSocialMedia != null ? new
                        {
                            u.Profile.ProfileSocialMedia.WebsiteUrl,
                            u.Profile.ProfileSocialMedia.InstagramUrl,
                            u.Profile.ProfileSocialMedia.FacebookUrl,
                            u.Profile.ProfileSocialMedia.TwitterUrl,
                            u.Profile.ProfileSocialMedia.TikTokUrl
                        } : null,

                        SocialMediaLinks = u.Profile.ProfileSocialMediaLinks
                            .Select(l => new ProfileSocialMediaLinkDto
                            {
                                Url = l.Url,
                                Title = l.Title,
                                Type = l.Type
                            })
                            .ToList(),

                        // Counts (calculated in database - FAST!)
                        FollowersCount = u.Profile.ProfileFollowers.Count(),
                        FollowingCount = u.Profile.ProfileFollowings.Count(),
                        PostsCount = u.Posts.Count(p => p.IsActive && p.MediaFile != null),

                        // ✅ OPTIMIZED: Calculate total likes efficiently
                        AllPostsLikesCount = u.Posts
                            .Where(p => p.IsActive && p.MediaFile != null)
                            .SelectMany(p => p.PostLikes)
                            .Count(pl => pl.IsActive),

                        // Stores
                        Stores = u.Stores.Select(s => new StoreDetailsResponse
                        {
                            Uid = s.Uid,
                            UniqueName = s.UniqueName,
                            Name = s.Name,
                            ImageUrl = s.ImageUrl,
                            Followers = s.StoreFollowers.Count() // ✅ Include followers count
                        }).ToList(),

                        // Currency (if exists)
                        Currency = u.Profile.CurrencyId != null
                            ? _dbContext.Currencies
                                .Where(c => c.Id == u.Profile.CurrencyId)
                                .Select(c => new CurrencyDetailsResponse
                                {
                                    Uid = c.Uid,
                                    Name = c.Name,
                                    Code = c.Code,
                                    Symbol = c.Symbol
                                })
                                .FirstOrDefault()
                            : null
                    })
                    .FirstOrDefaultAsync();

                if (profileData == null)
                {
                    throw new ForbiddenException();
                }

                // ✅ Map to response (all data already loaded - no more queries!)
                var response = new MyProfileDetailsResponse
                {
                    // User Info
                    Uid = profileData.Profile.Uid,
                    FullName = profileData.User.FirstName,
                    FirstName = profileData.User.FirstName,
                    LastName = profileData.User.LastName,
                    Username = profileData.User.UserName,
                    Email = profileData.User.Email,
                    PhoneNumber = profileData.User.PhoneNumber,

                    // Location
                    Address = profileData.User.Address,
                    ZipCode = profileData.User.ZipCode,
                    CityName = profileData.User.CityName,
                    Location = profileData.Country?.Name ?? profileData.Profile.Location,
                    CountryUid = profileData.Country?.Uid,

                    // Profile
                    ImageUrl = profileData.Profile.ImageUrl,
                    About = profileData.Profile.About,
                    Gender = profileData.Profile.GenderKey,
                    UserType = profileData.Profile.UserType,
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
                    //PostsCount = profileData.PostsCount,
                    PostsCount = profileData.AllPostsLikesCount,
                    AllPostsLikesCount = profileData.AllPostsLikesCount,

                    // Stores
                    Stores = profileData.Stores,
                    StoreUids = profileData.Stores.Select(s => s.Uid).ToList(), // ✅ Derived from Stores (no extra query)

                    // Currency
                    Currency = profileData.Currency,
                    CurrencyUid = profileData.Currency?.Uid
                };

                return response;
            }
            catch (Exception e)
            {
                if (e is Core.Application.Exceptions.BadRequestException ||
                    e is Core.Application.Exceptions.NotFoundException ||
                    e is Core.Application.Exceptions.ForbiddenException)
                {
                    throw;
                }

                throw new Exception($"Error retrieving profile: {e.Message}", e);
            }
        }

        public async Task<(string username, string uid)> ProfileToggleFollow(string profileUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser.Profile == null)
                {
                    throw new ForbiddenException($"Profile not found for user '{cUser.Id}'.");
                }

                var profile = await _dbContext.Profiles
                    .Include(p => p.User)
                    .Include(p => p.ProfileSettings)
                    .SingleOrDefaultAsync(p => p.IsActive && p.Uid == profileUid);
                if (profile == null)
                {
                    throw new BadRequestException($"Profile with uid '{profileUid}' doesn't exist.");
                }

                var follower =
                    await _dbContext.Profiles.SingleOrDefaultAsync(p => p.IsActive && p.Uid == cUser.Profile.Uid);
                if (follower == null)
                {
                    throw new BadRequestException($"Follower with uid '{cUser.Profile.Uid}' doesn't exist.");
                }

                // Check if either user has blocked the other
                var isBlocked = await _dbContext.UserBlocks
                    .AnyAsync(ub => 
                        (ub.BlockerProfileId == profileUid && ub.BlockedProfileId == cUser.Profile.Uid) ||
                        (ub.BlockerProfileId == cUser.Profile.Uid && ub.BlockedProfileId == profileUid) &&
                        ub.IsActive);

                if (isBlocked)
                {
                    throw new BadRequestException("Cannot follow this user as one of you has blocked the other.");
                }

                // Check if the profile is public or private
                // var IsProfilePublic  = profile.ProfileSettings?.IsProfilePublic ?? true;
                // if (!IsProfilePublic)
                // {
                //     var checkfollowSatatus = await _dbContext.ProfileFollowers
                //     .Where(pf => pf.Profile.Uid == profileUid && pf.Follower.Uid == cUser.Profile.Uid)
                //     .SingleOrDefaultAsync();
                //     if (checkfollowSatatus != null)
                //     {
                //         _dbContext.ProfileFollowers.Remove(checkfollowSatatus);

                //         var notification = await _dbContext.NotificationHistories
                //             .FirstOrDefaultAsync(n =>
                //                 n.TargetId == profileUid &&
                //                 n.TargetType == EntityTypeEnum.PROFILE &&
                //                 n.ActionType == NotificationActionTypeEnum.Follow &&
                //                 n.ActorUserId == cUser.Profile.Id);

                //         if (notification != null)
                //         {
                //             _dbContext.NotificationHistories.Remove(notification);
                //         }

                //         await _dbContext.SaveChangesAsync(CancellationToken.None);

                //         return (profile.User.UserName, profileUid);
                //     }
                //     //is profile is private , send a follow request instead
                //     var existingRequest = await _dbContext.FollowRequests
                //         .FirstOrDefaultAsync(fr => fr.RequesterProfileId == cUser.Profile.Uid && fr.TargetProfileId == profileUid && fr.IsActive);
                //     if (existingRequest != null)
                //         {
                //         throw new BadRequestException("Follow request already sent to this user.");
                //     }
                //     var followRequest = new FollowRequest
                //     {
                //         RequesterProfileId = cUser.Profile.Uid,
                //         TargetProfileId = profileUid,
                //         RequestedAt = DateTime.UtcNow,
                //         IsActive = true
                //     };
                //     _dbContext.FollowRequests.Add(followRequest);
                    
                //     // Create notification for follow request
                //     await _notificationService.SaveFollowRequestNotificationAsync(cUser.Profile.Uid, profileUid);
                    
                //     await _dbContext.SaveChangesAsync(CancellationToken.None);

                //     //when follow request is sent return a success message
                //     return (profile.User.UserName, profileUid);
                // }

                var pfm = await _dbContext.ProfileFollowers
                    .Where(pf => pf.Profile.Uid == profileUid && pf.Follower.Uid == cUser.Profile.Uid)
                    .SingleOrDefaultAsync();
                if (pfm != null)
                {
                    _dbContext.ProfileFollowers.Remove(pfm);

                    var notifications = await _dbContext.NotificationHistories
                        .Where(n =>
                            n.TargetId == profileUid &&
                            n.TargetType == EntityTypeEnum.PROFILE &&
                            n.ActionType == NotificationActionTypeEnum.Follow &&
                            n.ActorUserId == cUser.Profile.Id)
                        .ToListAsync();

                    if (notifications.Any())
                    {
                        _dbContext.NotificationHistories.RemoveRange(notifications);
                    }
                }
                else
                {
                    _dbContext.ProfileFollowers.Add(new ProfileFollower() { Profile = profile, Follower = follower });
                    await _notificationService.SaveFollowNotificationAsync(follower.UserId,profile.UserId,profileUid);

                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return (profile.User.UserName, profile.Uid);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<(string username, string uid)> AcceptFollowRequest(string requesterProfileUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                if (cUser == null)
                {
                    throw new NotAuthenticatedException();
                }

                // Find the follow request
                var followRequest = await _dbContext.FollowRequests
                    .FirstOrDefaultAsync(fr => fr.RequesterProfileId == requesterProfileUid && 
                                             fr.TargetProfileId == cUser.Profile.Uid && 
                                             fr.IsActive);

                if (followRequest == null)
                {
                    throw new BadRequestException("Follow request not found or already processed.");
                }

                // Get the requester's profile
                var requesterProfile = await _dbContext.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Uid == requesterProfileUid && p.IsActive);

                if (requesterProfile == null)
                {
                    throw new BadRequestException("Requester profile not found.");
                }

                // Create the follow relationship
                _dbContext.ProfileFollowers.Add(new ProfileFollower() 
                { 
                    Profile = cUser.Profile, 
                    Follower = requesterProfile 
                });

                // Mark the follow request as inactive
                followRequest.IsActive = false;

                // Send follow notification to the requester
                // await _notificationService.SaveFollowNotificationAsync(requesterProfile.UserId, cUser.Profile.UserId, cUser.Profile.Uid);

                // Send follow request accepted notification to the requester (includes follow-back button logic)
                await _notificationService.SaveFollowRequestAcceptedNotificationAsync(cUser.Profile.UserId, requesterProfile.UserId, cUser.Profile.Uid);

                // Remove the follow request notification
                var notification = await _dbContext.NotificationHistories
                    .FirstOrDefaultAsync(n =>
                        n.TargetId == cUser.Profile.Uid &&
                        n.TargetType == EntityTypeEnum.PROFILE &&
                        n.ActionType == NotificationActionTypeEnum.FollowRequest &&
                        n.ActorUserId == requesterProfile.Id);

                if (notification != null)
                {
                    _dbContext.NotificationHistories.Remove(notification);
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return (requesterProfile.User.UserName, requesterProfile.Uid);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<(string username, string uid)> RejectFollowRequest(string requesterProfileUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser == null)
                {
                    throw new NotAuthenticatedException();
                }

                // Find the follow request
                var followRequest = await _dbContext.FollowRequests
                    .FirstOrDefaultAsync(fr => fr.RequesterProfileId == requesterProfileUid && 
                                             fr.TargetProfileId == cUser.Profile.Uid && 
                                             fr.IsActive);

                if (followRequest == null)
                {
                    throw new BadRequestException("Follow request not found or already processed.");
                }

                // Get the requester's profile
                var requesterProfile = await _dbContext.Profiles
                    .FirstOrDefaultAsync(p => p.Uid == requesterProfileUid && p.IsActive);

                if (requesterProfile == null)
                {
                    throw new BadRequestException("Requester profile not found.");
                }

                // Mark the follow request as inactive
                followRequest.IsActive = false;

                // Remove the follow request notification
                var notification = await _dbContext.NotificationHistories
                    .FirstOrDefaultAsync(n =>
                        n.TargetId == cUser.Profile.Uid &&
                        n.TargetType == EntityTypeEnum.PROFILE &&
                        n.ActionType == NotificationActionTypeEnum.FollowRequest &&
                        n.ActorUserId == requesterProfile.Id);

                if (notification != null)
                {
                    _dbContext.NotificationHistories.Remove(notification);
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return (cUser.Profile.User.UserName, cUser.Profile.Uid);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<(string username, string uid)> ToggleFollowRequest(string targetProfileUid)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser == null)
                {
                    throw new NotAuthenticatedException();
                }

                var profile = await _dbContext.Profiles
                    .Include(p => p.User)
                    .Include(p => p.ProfileSettings)
                    .SingleOrDefaultAsync(p => p.IsActive && p.Uid == targetProfileUid);
                if (profile == null)
                {
                    throw new BadRequestException($"Profile with uid '{targetProfileUid}' doesn't exist.");
                }

                // Check if this is a followback request (target follows current user)
                var isFollowBackRequest = await _dbContext.ProfileFollowers
                    .AnyAsync(pf => pf.Profile.Uid == cUser.Profile.Uid && pf.Follower.Uid == targetProfileUid);

                // Check if the profile is private OR if this is a followback request
                var IsProfilePublic = profile.ProfileSettings?.IsProfilePublic ?? true;
                if (IsProfilePublic && !isFollowBackRequest)
                {
                    throw new BadRequestException("Cannot use toggle-follow-request for public profiles unless it's a followback request.");
                }

                // Check if either user has blocked the other
                var isBlocked = await _dbContext.UserBlocks
                    .AnyAsync(ub => 
                        (ub.BlockerProfileId == targetProfileUid && ub.BlockedProfileId == cUser.Profile.Uid) ||
                        (ub.BlockerProfileId == cUser.Profile.Uid && ub.BlockedProfileId == targetProfileUid) &&
                        ub.IsActive);

                if (isBlocked)
                {
                    throw new BadRequestException("Cannot follow this user as one of you has blocked the other.");
                }

                // Check if already following (in case they were following before profile became private)
                var existingFollow = await _dbContext.ProfileFollowers
                    .Where(pf => pf.Profile.Uid == targetProfileUid && pf.Follower.Uid == cUser.Profile.Uid)
                    .SingleOrDefaultAsync();
                
                if (existingFollow != null)
                {
                    // Unfollow
                    _dbContext.ProfileFollowers.Remove(existingFollow);

                    var followNotifications = await _dbContext.NotificationHistories
                        .Where(n =>
                            n.TargetId == targetProfileUid &&
                            n.TargetType == EntityTypeEnum.PROFILE &&
                            n.ActionType == NotificationActionTypeEnum.Follow &&
                            n.ActorUserId == cUser.Profile.Id)
                        .ToListAsync();

                    if (followNotifications.Any())
                    {
                        _dbContext.NotificationHistories.RemoveRange(followNotifications);
                    }

                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    return (profile.User.UserName, targetProfileUid);
                }

                // Check if follow request already exists
                var existingRequest = await _dbContext.FollowRequests
                    .FirstOrDefaultAsync(fr => fr.RequesterProfileId == cUser.Profile.Uid && fr.TargetProfileId == targetProfileUid && fr.IsActive);

                if (existingRequest != null)
                {
                    // Cancel the follow request
                    existingRequest.IsActive = false;

                    // Remove all matching follow request notifications
                    var requestNotifications = await _dbContext.NotificationHistories
                        .Where(n =>
                            n.TargetId == targetProfileUid &&
                            n.TargetType == EntityTypeEnum.PROFILE &&
                            n.ActionType == NotificationActionTypeEnum.FollowRequest &&
                            n.ActorUserId == cUser.Profile.Id)
                        .ToListAsync();

                    if (requestNotifications.Any())
                    {
                        _dbContext.NotificationHistories.RemoveRange(requestNotifications);
                    }

                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    return (profile.User.UserName, targetProfileUid);
                }
                else
                {
                    // Send follow request
                    var followRequest = new FollowRequest
                    {
                        RequesterProfileId = cUser.Profile.Uid,
                        TargetProfileId = targetProfileUid,
                        RequestedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _dbContext.FollowRequests.Add(followRequest);
                    
                    // Create notification for follow request (works for both regular and followback requests)
                    await _notificationService.SaveFollowRequestNotificationAsync(cUser.Profile.Uid, targetProfileUid);
                    
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    return (profile.User.UserName, targetProfileUid);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<string> ProfileUpdateAvatarImage(Profile profile, IFormFile image)
        {
            try
            {
                if (profile == null)
                {
                    throw new ForbiddenException("Profile cannot be null.");
                }

                // Handle image removal case
                if (image == null)
                {
                    if (profile.ImageUrl != null)
                    {
                        string bucketName = _configuration[AwsLocationNames.S3UploadBucket];
                        string folderPath = _configuration[AwsLocationNames.PublicUploadFolder];

                        var fileConfig = new FileUploadConfigDto()
                        {
                            BucketName = bucketName,
                            FolderPath = folderPath,
                            OldFileName = profile.ImageUrl.Substring(profile.ImageUrl.LastIndexOf("/") + 1),
                        };

                        await _fileUploadService.Delete(fileConfig);
                    }

                    profile.ImageUrl = null;
                    await _dbContext.SaveChangesAsync(CancellationToken.None);
                    return string.Empty;
                }

                // Handle image upload case
                string uploadBucketName = _configuration[AwsLocationNames.S3UploadBucket];
                string uploadFolderPath = _configuration[AwsLocationNames.PublicUploadFolder];

                var uploadFileConfig = new FileUploadConfigDto()
                {
                    FileName = image.FileName,
                    BucketName = uploadBucketName,
                    FolderPath = uploadFolderPath,
                    File = image,
                    ImageWidth = PulrGlobalConfig.AvatarImage.Width,
                    ImageHeight = PulrGlobalConfig.AvatarImage.Height,
                };

                if (profile.ImageUrl != null)
                {
                    uploadFileConfig.OldFileName = profile.ImageUrl.Substring(profile.ImageUrl.LastIndexOf("/") + 1);
                    await _fileUploadService.Delete(uploadFileConfig);
                }

                string path = await _fileUploadService.UploadImage(uploadFileConfig);

                profile.ImageUrl = path;
                // Remove leading slash if present
                if (!string.IsNullOrEmpty(path) && path.StartsWith("/"))
                {
                    path = path.Substring(1);
                }

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                return path;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<List<string>> SearchHandles(string search)
        {
            try
            {
                var handles = new List<string>();

                var profileHandles = await _dbContext.Profiles
                    .Where(p => p.User.UserName.StartsWith(search) && p.IsActive).Take(10).Select(p => p.User.UserName)
                    .ToListAsync();
                handles.AddRange(profileHandles);
                var storeHandles = await _dbContext.Stores.Where(s => s.UniqueName.StartsWith(search) && s.IsActive)
                    .Take(10).Select(s => s.UniqueName).ToListAsync();
                handles.AddRange(storeHandles);
                return handles;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<string> Update(ProfileUpdateDto model)
        {
            try
            {
                var username = UsernameHelper.Normalize(model.Username);
                var user = await _currentUserService.GetUserAsync();
                var changes = new List<string>();
                var warnings = new List<string>();

                if (user?.Profile == null)
                {
                    throw new BadRequestException("Profile not found.");
                }

                if (!String.IsNullOrWhiteSpace(model.FirstName) && model.FirstName != user.FirstName)
                {
                    user.FirstName = model.FirstName;
                    changes.Add("First name");
                }

                if (!String.IsNullOrWhiteSpace(model.LastName) && model.LastName != user.LastName)
                {
                    user.LastName = model.LastName;
                    changes.Add("Last name");
                }

                if (!String.IsNullOrWhiteSpace(model.DisplayName) && model.DisplayName != user.DisplayName)
                {
                    try
                    {
                        CheckIfDisplayNameChangeIsAllowed(user);
                        user.DisplayName = model.DisplayName;
                        changes.Add("Display name");
                    }
                    catch (Exception)
                    {
                        var daysUntilAvailable = 30 - (int)(DateTime.UtcNow - user.DisplayNameChangeDate).TotalDays;
                        var message = $"Display name can't be changed for another {daysUntilAvailable} days.";
                        var vf = new ValidationFailure(nameof(user.DisplayName), message);
                        throw new ValidationException(new List<ValidationFailure>() { vf }, message);
                    }
                }

                if (!string.IsNullOrEmpty(model.Location) && model.Location != user.Profile.Location)
                {
                    user.Profile.Location = model.Location;
                    changes.Add("Location");
                }
                if (!string.IsNullOrEmpty(model.UserType) && model.UserType != user.Profile.UserType)
                {
                    user.Profile.UserType = model.UserType;
                    changes.Add("UserType");
                }               

                if (!String.IsNullOrWhiteSpace(username) && username != user.UserName)
                {
                    try
                    {
                        CheckIfUsernameChangeIsAllowed(user);

                        bool usernameTaken =
                            await _dbContext.Users.AnyAsync(u => u.UserName == username && u.Id != user.Id);
                        if (usernameTaken)
                        {
                            throw new ForbiddenException("Username taken.");
                        }

                        user.UserName = username;
                        changes.Add("Username");
                    }
                    catch (Exception)
                    {
                        var daysUntilAvailable = 30 - (int)(DateTime.UtcNow - user.UsernameChangeDate).TotalDays;
                        var message = $"Username can't be changed for another {daysUntilAvailable} days.";
                        var vf = new ValidationFailure(nameof(user.UserName), message);
                        throw new ValidationException(new List<ValidationFailure>() { vf }, message);
                    }
                }

                user.Address = model.Address ?? user.Address;
                user.ZipCode = model.ZipCode ?? user.ZipCode;
                user.CityName = model.CityName ?? user.CityName;
                user.Country = await _dbContext.Countries.SingleOrDefaultAsync(c => c.Uid == model.CountryUid);
                user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;

                // Ensure ProfileSocialMedia is not null before setting its properties
                if (user.Profile.ProfileSocialMedia == null)
                {
                    user.Profile.ProfileSocialMedia = new ProfileSocialMedia
                    {
                        ProfileId = user.Profile.Id
                    };
                }
                user.Profile.ProfileSocialMedia.WebsiteUrl = model.WebsiteUrl;
                user.Profile.ProfileSocialMedia.FacebookUrl = model.FacebookUrl;
                user.Profile.ProfileSocialMedia.TikTokUrl = model.TikTokUrl;
                user.Profile.ProfileSocialMedia.InstagramUrl = model.InstagramUrl;
                user.Profile.ProfileSocialMedia.TwitterUrl = model.TwitterUrl;

                 if (model.SocialMediaLinks != null && model.SocialMediaLinks.Any())
            {
                foreach (var link in model.SocialMediaLinks)
                {
                    // Find existing link with same type
                    var existingLink = user.Profile.ProfileSocialMediaLinks
                        .FirstOrDefault(l => l.Type == link.Type);

                    if (existingLink != null)
                    {
                        // Update existing link
                        existingLink.Url = link.Url;
                        existingLink.Title = link.Title;
                        changes.Add($"{link.Type} social media link");
                    }
                    else
                    {
                        // Create new link
                        user.Profile.ProfileSocialMediaLinks.Add(new ProfileSocialMediaLink
                        {
                            Url = link.Url,
                            Title = link.Title,
                            Type = link.Type,
                            ProfileId = user.Profile.Id
                        });
                        changes.Add($"New {link.Type} social media link");
                    }
                }
            }

                user.Profile.About = model.About ?? user.Profile.About;
                if (!String.IsNullOrWhiteSpace(model.CurrencyUid))
                {
                    user.Profile.Currency =
                        await _dbContext.Currencies.SingleOrDefaultAsync(c => c.Uid == model.CurrencyUid);
                }
                else if (user.Profile.Currency == null)
                {
                    user.Profile.Currency = await _dbContext.Currencies.SingleOrDefaultAsync(c =>
                        c.Code == _configuration["ProfileSettings:DefaultCurrencyCode"]);
                }

                var genderEntity = model.Gender == null
                    ? await _dbContext.Genders.SingleOrDefaultAsync(g => g.Key == GenderEnum.Other.ToString())
                    : await _dbContext.Genders.SingleOrDefaultAsync(g => g.Key == model.Gender.ToString());

                await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                user.Profile.Gender = genderEntity;

                await _dbContext.SaveChangesAsync(CancellationToken.None);

                if (changes.Any())
                {
                    var message = $"Changes updated: {string.Join(", ", changes)}";
                    if (warnings.Any())
                    {
                        message += $". {string.Join(". ", warnings)}";
                    }
                    return message;
                }
                else if (warnings.Any())
                {
                    return $"No changes were made. {string.Join(". ", warnings)}";
                }
                else
                {
                    return "No changes were made";
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        public async Task<List<ProfileResponse>> MapProfileResponseList(IQueryable<Profile> profiles, CancellationToken ct)
        {
            var profilesResponse =  await profiles.Select(p => new ProfileResponse
            {
                FullName = p.User.FirstName,
                FirstName = p.User.FirstName,
                LastName = p.User.LastName,
                Followers = p.ProfileFollowers.Count(),
                Following = p.ProfileFollowings.Count(),
                UserType = p.User.Profile.UserType,
                ImageUrl = p.ImageUrl,
                UserId = p.UserId,
                Uid = p.Uid,
                Username = p.User.UserName
            }).ToListAsync(ct);
            foreach (var item in profilesResponse)
            {
                item.IsInfluencer = await _userManager.IsInRoleAsync(new User { Id = item.UserId }, PulrRoles.Influencer);
            }

            return profilesResponse;
        }

        public async Task<User> GetCurrentUserWithProfile()
        {
            var user = await _currentUserService.GetUserAsync();
            if (user == null)
                return null;

            var dbUser = await _dbContext.Users
                .Where(u => u.Id == user.Id)
                .Include(u => u.Profile)
                    .ThenInclude(p => p.ProfileSocialMedia)
                .Include(u => u.Profile)
                    .ThenInclude(p => p.ProfileSocialMediaLinks)
                .SingleOrDefaultAsync();

            return dbUser;
        }

        private void CheckIfUsernameChangeIsAllowed(User user)
        {
            if (user.UsernameChangesCount == 0)
            {
                // First change after registration is allowed
                user.UsernameChangeDate = DateTime.UtcNow;
                user.UsernameChangesCount = 1;
            }
            else
            {
                // Only allow change if 30 days have passed since last change
                if (user.UsernameChangeDate > DateTime.UtcNow.AddDays(-30))
                {
                    var daysUntilAvailable = 30 - (int)(DateTime.UtcNow - user.UsernameChangeDate).TotalDays;
                    var vf = new ValidationFailure(nameof(user.UserName),
                        $"Username can't be changed for another {daysUntilAvailable} days.");
                    throw new ValidationException(new List<ValidationFailure>() { vf });
                }
                user.UsernameChangeDate = DateTime.UtcNow;
                user.UsernameChangesCount++;
            }
        }

        private void CheckIfDisplayNameChangeIsAllowed(User user)
        {
            if (user.DisplayNameChangeDate > DateTime.UtcNow.AddDays(-30) && user.DisplayNameChangesCount >= 1)
            {
                var daysUntilAvailable =
                    Convert.ToInt32(30 - (DateTime.UtcNow.Subtract(user.DisplayNameChangeDate).TotalDays));
                var vf = new ValidationFailure(nameof(user.DisplayName),
                    "Display name can't be changed in next " + daysUntilAvailable + " days.");
                throw new ValidationException(new List<ValidationFailure>() { vf });
            }
            else if (user.DisplayNameChangeDate < DateTime.UtcNow.AddDays(-30) && user.DisplayNameChangesCount >= 1)
            {
                user.DisplayNameChangesCount = 0;
            }

            if (user.DisplayNameChangesCount < 1)
            {
                if (user.DisplayNameChangesCount == 0)
                {
                    user.DisplayNameChangeDate = DateTime.UtcNow;
                }

                user.DisplayNameChangesCount += 1;
            }
        }
    }
}
