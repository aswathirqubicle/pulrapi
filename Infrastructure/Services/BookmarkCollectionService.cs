using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.BookmarkCollections;
using Core.Application.Models.Post;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using Core.Infrastructure.Services.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Infrastructure.Services
{
    public class BookmarkCollectionService : IBookmarkCollectionService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;

        public BookmarkCollectionService(
            IApplicationDbContext dbContext, 
            IMapper mapper,
            ICurrentUserService currentUserService,
            INotificationService notificationService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Validates profile UID using OptimizedValidationBase for consistent validation
        /// </summary>
        private void ValidateProfileUid(string profileUid)
        {
            var safeUidAttribute = new SafeUidAttribute(allowNullValue: true, maxLength: 50, minLength: 1, validateGuidFormat: true);
            var validationContext = new ValidationContext(new { ProfileUid = profileUid });
            var validationResult = safeUidAttribute.GetValidationResult(profileUid, validationContext);
            
            if (validationResult != ValidationResult.Success)
            {
                throw new Core.Application.Exceptions.BadRequestException(validationResult.ErrorMessage);
            }
        }

        public async Task<BookmarkCollectionResponse> CreateCollectionAsync(string name, string profileId, string postId = null)
        {
            var profile = await _dbContext.Profiles
                .SingleOrDefaultAsync(p => p.Uid == profileId);

            // Ensure user has a "Saved" collection - create one if it doesn't exist
            await EnsureSavedCollectionExistsAsync(profile);

            var collectionItems = new List<BookmarkCollectionItem>();
            string newPostPreviewImage = null;
            // Check if collection exists
            var collection = await _dbContext.BookmarkCollections
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                        .ThenInclude(p => p.MediaFile)
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                        .ThenInclude(p => p.BookmarkCollectionItems)
                            .ThenInclude(bci2 => bci2.BookmarkCollection)
                .FirstOrDefaultAsync(c => c.ProfileId == profile.Id && c.Name.ToLower() == name.ToLower());

            if (collection != null)
            {
                // If postId is provided, add the post to the existing collection
                if (!string.IsNullOrEmpty(postId))
                {
                    //check the postId is valid in db
                    var checkpost = await _dbContext.Posts.FirstOrDefaultAsync(p => p.Uid == postId);
                    if (checkpost == null)
                    {
                        throw new Core.Application.Exceptions.BadRequestException("Post ID is not valid.");
                    }

                    await AddPostToCollectionAsync(postId, collection.Uid, profileId);
                    // Reload collection with bookmarks
                    collectionItems = await _dbContext.BookmarkCollectionItems
                        .Where(bci => bci.BookmarkCollectionId == collection.Id)
                        .Include(bci => bci.Post)
                            .ThenInclude(p => p.MediaFile)
                        .Include(bci => bci.Post)
                            .ThenInclude(p => p.BookmarkCollectionItems)
                                .ThenInclude(bci2 => bci2.BookmarkCollection)
                        .ToListAsync();

                    // Get the newly added post's preview image
                    var post = await _dbContext.Posts
                        .Include(p => p.MediaFile)
                        .FirstOrDefaultAsync(p => p.Uid == postId);

                    if (post?.MediaFile != null)
                        newPostPreviewImage = post.MediaFile.Url;
                }
                // Return the existing collection (with or without the new post)
                return new BookmarkCollectionResponse
                {
                    Uid = collection.Uid,
                    Name = collection.Name,
                    PostsCount = collection.BookmarkCollectionItems?.Count ?? 0,
                    Items = collection.BookmarkCollectionItems?
                        .Where(bci => bci.Post != null)
                        .Select(bci => {
                            var postResponse = _mapper.Map<PostResponse>(bci.Post);
                            // Set bookmark status for current user (check both bookmarks and collections)
                            var currentUser = _currentUserService.GetUserAsync().Result;
                            if (currentUser?.Profile != null)
                            {
                                postResponse.BookmarkedByMe = bci.Post.BookmarkCollectionItems?.Any(bci2 => bci2.BookmarkCollection.ProfileId == currentUser.Profile.Id && bci2.BookmarkCollection.IsActive) ?? false;
                            }
                            return postResponse;
                        }).ToList() ?? new List<PostResponse>(),
                    PreviewImages = !string.IsNullOrEmpty(newPostPreviewImage)
                        ? new List<string> { newPostPreviewImage }
                        : new List<string>(),
                    PostUids = collection.BookmarkCollectionItems?
                        .Where(bci => bci.Post != null)
                        .Select(bci => bci.Post.Uid)
                        .ToList() ?? new List<string>()
                };
            }

            // If collection does not exist, create it
            collection = new BookmarkCollection
            {
                Name = name,
                ProfileId = profile.Id,
                ProfileUid = profile.Uid,
            };
            _dbContext.BookmarkCollections.Add(collection);
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            // If a postId is provided, add the post to the new collection
            if (!string.IsNullOrEmpty(postId))
            {
                await AddPostToCollectionAsync(postId, collection.Uid, profileId);
                // Reload collection with bookmarks
                collectionItems = await _dbContext.BookmarkCollectionItems
                    .Where(bci => bci.BookmarkCollectionId == collection.Id)
                    .Include(bci => bci.Post)
                        .ThenInclude(p => p.MediaFile)
                    .Include(bci => bci.Post)
                        .ThenInclude(p => p.BookmarkCollectionItems)
                            .ThenInclude(bci2 => bci2.BookmarkCollection)
                    .ToListAsync();

                // Get the newly added post's preview image
                var post = await _dbContext.Posts
                    .Include(p => p.MediaFile)
                    .FirstOrDefaultAsync(p => p.Uid == postId);

                if (post?.MediaFile != null)
                    newPostPreviewImage = post.MediaFile.Url;
            }

            return new BookmarkCollectionResponse
            {
                Uid = collection.Uid,
                Name = collection.Name,
                PostsCount = collectionItems?.Count ?? 0,
                Items = collectionItems?
                    .Where(bci => bci.Post != null)
                    .Select(bci => {
                        var postResponse = _mapper.Map<PostResponse>(bci.Post);
                        // Set bookmark status for current user (check both bookmarks and collections)
                        var currentUser = _currentUserService.GetUserAsync().Result;
                        if (currentUser?.Profile != null)
                        {
                            postResponse.BookmarkedByMe = bci.Post.BookmarkCollectionItems?.Any(bci2 => bci2.BookmarkCollection.ProfileId == currentUser.Profile.Id && bci2.BookmarkCollection.IsActive) ?? false;
                        }
                        return postResponse;
                    }).ToList() ?? new List<PostResponse>(),
                PreviewImages = !string.IsNullOrEmpty(newPostPreviewImage)
                    ? new List<string> { newPostPreviewImage }
                    : new List<string>(),
                PostUids = collectionItems?
                    .Where(bci => bci.Post != null)
                    .Select(bci => bci.Post.Uid)
                    .ToList() ?? new List<string>()
            };
        }

        public async Task<BookmarkCollectionResponse> UpdateCollectionAsync(string Uid, string name, string profileId)
        {
            // No need to prevent users from renaming to "Saved_c21e61b09db9" as it's unique enough

            var profile = await _dbContext.Profiles
                .SingleOrDefaultAsync(p => p.Uid == profileId);

            // Ensure user has a "Saved" collection - create one if it doesn't exist
            await EnsureSavedCollectionExistsAsync(profile);

            var collection = await _dbContext.BookmarkCollections
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                        .ThenInclude(p => p.MediaFile)
                .SingleOrDefaultAsync(c => c.Uid == Uid && c.Profile.Uid == profileId);
            if (collection == null) throw new NotFoundException("Collection not found");
            
            // Prevent changing the name of the "Saved" collection (case-insensitive)
            if (string.Equals(collection.Name, "Saved", StringComparison.OrdinalIgnoreCase))
            {
                throw new Core.Application.Exceptions.BadRequestException("Cannot rename the default 'Saved' collection.");
            }
            
            collection.Name = name;
            collection.UpdatedAt = System.DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(CancellationToken.None);

            var items = collection.BookmarkCollectionItems?
                .Where(bci => bci.Post != null)
                .Select(bci => _mapper.Map<PostResponse>(bci.Post))
                .ToList() ?? new List<PostResponse>();

            return new BookmarkCollectionResponse
            {
                Uid = collection.Uid,
                Name = collection.Name,
                PostsCount = collection.BookmarkCollectionItems?.Count ?? 0,
                Items = items,
                PreviewImages = collection.BookmarkCollectionItems?
                    .Where(bci => bci.Post != null && bci.Post.MediaFile != null)
                    .Take(4)
                    .Select(bci => string.IsNullOrEmpty(bci.Post.ThumbnailUrl) ? (bci.Post.MediaFile.OriginalUrl ?? bci.Post.MediaFile.Url) : bci.Post.ThumbnailUrl)
                    .ToList() ?? new List<string>(),
                PostUids = collection.BookmarkCollectionItems?
                    .Where(bci =>  bci.Post != null)
                    .Select(bci => bci.Post.Uid)
                    .ToList() ?? new List<string>()
            };
        }

        public async Task DeleteCollectionAsync(string Uid, string profileId)
        {
            ValidateProfileUid(Uid);
            
            try
            {
                var profile = await _dbContext.Profiles
                    .SingleOrDefaultAsync(p => p.Uid == profileId);

                // Ensure user has a "Saved" collection - create one if it doesn't exist
                await EnsureSavedCollectionExistsAsync(profile);

                // First, get the collection to check if it's the "Saved" collection
                var collection = await _dbContext.BookmarkCollections
                    .FirstOrDefaultAsync(c => c.Uid == Uid && c.Profile.Uid == profileId);
                    
                if (collection == null) throw new NotFoundException("Collection not found");

                // Prevent deletion of the "Saved" collection
                if (string.Equals(collection.Name, "Saved", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Core.Application.Exceptions.BadRequestException("Cannot delete the default 'Saved' collection.");
                }

                var collectionId = collection.Id;
                
                // Handle stories that reference this collection
                var storiesWithCollection = await _dbContext.Stories
                    .Where(s => s.SharedCollectionId == collectionId)
                    .ToListAsync();
                    
                if (storiesWithCollection.Any())
                {
                    // Set SharedCollectionId to null for stories that reference this collection
                    foreach (var story in storiesWithCollection)
                    {
                        story.SharedCollectionId = null;
                    }
                    _dbContext.Stories.UpdateRange(storiesWithCollection);
                }
                
                // Remove all collection items for this collection
                var collectionItems = await _dbContext.Set<BookmarkCollectionItem>()
                    .Where(bci => bci.BookmarkCollectionId == collectionId)
                    .ToListAsync();
                    
                if (collectionItems.Any())
                {
                    _dbContext.Set<BookmarkCollectionItem>().RemoveRange(collectionItems);
                }
                
                // Remove the collection itself
                //var collection = await _dbContext.BookmarkCollections
                //    .FirstOrDefaultAsync(c => c.Id == collectionId);
                    
                if (collection != null)
                {
                    _dbContext.BookmarkCollections.Remove(collection);
                }
                
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Re-throw application exceptions (BadRequestException, NotFoundException) as-is
                if (ex is Core.Application.Exceptions.BadRequestException || 
                    ex is Core.Application.Exceptions.NotFoundException)
                {
                    throw;
                }
                
                // Log and wrap other exceptions
                throw new Exception($"Error deleting collection {Uid} for profile {profileId}: {ex.Message}", ex);
            }
        }

        public async Task AddPostToCollectionAsync(string postId, string collectionUid, string profileId)
        {
            //get profileId from profileUid
            var profileid =  await _dbContext.Profiles
                .Where(p => p.Uid == profileId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var postid = await _dbContext.Posts
                .Where(p => p.Uid == postId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            // Find collection
            var collection = await _dbContext.BookmarkCollections.FirstOrDefaultAsync(c => c.Uid == collectionUid && c.Profile.Uid == profileId);
            if (collection == null) throw new KeyNotFoundException("Collection not found");

            // Add to collection if not already present
            var exists = await _dbContext.Set<BookmarkCollectionItem>()
                .AnyAsync(bci => bci.PostId == postid && bci.BookmarkCollectionId == collection.Id);
            if (!exists)
            {
                _dbContext.Set<BookmarkCollectionItem>().Add(new BookmarkCollectionItem
                {
                    PostId = postid,
                    BookmarkCollectionId = collection.Id
                });
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
        }

        public async Task RemovePostFromCollectionAsync(string postId, string collectionUid, string profileId)
        {
            //get profileId from profileUid
            var profileid = await _dbContext.Profiles
                .Where(p => p.Uid == profileId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var postid = await _dbContext.Posts
                .Where(p => p.Uid == postId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var collection = await _dbContext.BookmarkCollections.FirstOrDefaultAsync(c => c.Uid == collectionUid && c.Profile.Uid == profileId);
            if (collection == null) throw new KeyNotFoundException("Collection not found");
            
            var item = await _dbContext.Set<BookmarkCollectionItem>()
                .FirstOrDefaultAsync(bci => bci.PostId == postid && bci.BookmarkCollectionId == collection.Id);
            if (item != null)
            {
                _dbContext.Set<BookmarkCollectionItem>().Remove(item);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
        }

        public async Task<List<BookmarkCollectionResponse>> SearchCollectionsAsync(string searchTerm)
        {
            var cUser = await _currentUserService.GetUserAsync();

            // get profileId from profileUid
            var profileUid = cUser.Profile.Uid;

            // Ensure user has a "Saved" collection - create one if it doesn't exist
            await EnsureSavedCollectionExistsAsync(cUser.Profile);

            // Find collections for the current user profile matching the search term
            var collections = await _dbContext.BookmarkCollections
                .Where(c => c.Profile.Uid == profileUid && c.IsActive && c.Name.ToLower().Contains(searchTerm.ToLower()))
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                        .ThenInclude(p => p.MediaFile)
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                        .ThenInclude(p => p.BookmarkCollectionItems)
                            .ThenInclude(bci2 => bci2.BookmarkCollection)
                .ToListAsync();

            // Separate "Saved" collection from other collections
            var savedCollection = collections.FirstOrDefault(c => c.Name.ToLower() == "saved");
            var otherCollections = collections.Where(c => c.Name.ToLower() != "saved").ToList();

            var result = new List<BookmarkCollectionResponse>();

            // Add "Saved" collection first if it matches the search term
            if (savedCollection != null)
            {
                result.Add(new BookmarkCollectionResponse
                {
                    Uid = savedCollection.Uid,
                    Name = "Saved",
                    PostsCount = savedCollection.BookmarkCollectionItems?.Count ?? 0,
                    Items = savedCollection.BookmarkCollectionItems?
                        .Where(bci => bci.Post != null)
                        .Select(bci => {
                            var postResponse = _mapper.Map<PostResponse>(bci.Post);
                            // Set bookmark status for current user (check both bookmarks and collections)
                            var currentUser = _currentUserService.GetUserAsync().Result;
                            if (currentUser?.Profile != null)
                            {
                                postResponse.BookmarkedByMe = bci.Post.BookmarkCollectionItems?.Any(bci2 => bci2.BookmarkCollection.ProfileId == currentUser.Profile.Id && bci2.BookmarkCollection.IsActive) ?? false;
                            }
                            return postResponse;
                        }).ToList() ?? new List<PostResponse>(),
                    PreviewImages = savedCollection.BookmarkCollectionItems?
                        .Where(bci => bci.Post != null && bci.Post.MediaFile != null)
                        .Take(4)
                        .Select(bci => string.IsNullOrEmpty(bci.Post.ThumbnailUrl) ? (bci.Post.MediaFile.OriginalUrl ?? bci.Post.MediaFile.Url) : bci.Post.ThumbnailUrl)
                        .ToList() ?? new List<string>(),
                    PostUids = savedCollection.BookmarkCollectionItems?
                        .Where(bci => bci.Post != null)
                        .Select(bci => bci.Post.Uid)
                        .ToList() ?? new List<string>()
                });
            }

            // Add other collections
            foreach (var collection in otherCollections)
            {
                result.Add(new BookmarkCollectionResponse
            {
                Uid = collection.Uid,
                Name = collection.Name,
                PostsCount = collection.BookmarkCollectionItems?.Count ?? 0,
                Items = collection.BookmarkCollectionItems?
                    .Where(bci => bci.Post != null)
                    .Select(bci => {
                        var postResponse = _mapper.Map<PostResponse>(bci.Post);
                        // Set bookmark status for current user (check both bookmarks and collections)
                        var currentUser = _currentUserService.GetUserAsync().Result;
                        if (currentUser?.Profile != null)
                        {
                            postResponse.BookmarkedByMe = bci.Post.BookmarkCollectionItems?.Any(bci2 => bci2.BookmarkCollection.ProfileId == currentUser.Profile.Id && bci2.BookmarkCollection.IsActive) ?? false;
                        }
                        return postResponse;
                    }).ToList() ?? new List<PostResponse>(),
                PreviewImages = collection.BookmarkCollectionItems?
                    .Where(bci => bci.Post != null && bci.Post.MediaFile != null)
                    .Take(4)
                    .Select(bci => string.IsNullOrEmpty(bci.Post.ThumbnailUrl) ? (bci.Post.MediaFile.OriginalUrl ?? bci.Post.MediaFile.Url) : bci.Post.ThumbnailUrl)
                    .ToList() ?? new List<string>(),
                PostUids = collection.BookmarkCollectionItems?
                    .Where(bci => bci.Post != null)
                    .Select(bci => bci.Post.Uid)
                    .ToList() ?? new List<string>()
                });
            }

            return result;
        }

        public async Task<BookmarkCollectionResponse> GetCollectionByUidAsync(string Uid)
        {
            var collection = await _dbContext.BookmarkCollections
                .Include(c => c.Profile)
                .FirstOrDefaultAsync(c => c.Uid == Uid);
            if (collection == null) return null;

            if (collection.Profile == null)
            {
                collection.Profile = await _dbContext.Profiles
                    .FirstOrDefaultAsync(p => p.Id == collection.ProfileId);
            }

            // Privacy: if the collection owner profile is private, allow only owner or followers
            var isPrivate = await _dbContext.ProfileSettings
                .Where(ps => ps.ProfileId == collection.ProfileId)
                .Select(ps => !ps.IsProfilePublic)
                .FirstOrDefaultAsync();

            if (isPrivate)
            {
                var currentUser = await _currentUserService.GetUserAsync();
                var isOwner = currentUser?.Profile?.Id == collection.ProfileId;
                var isFollower = false;
                if (!isOwner && currentUser?.Profile != null)
                {
                    isFollower = await _dbContext.ProfileFollowers.AnyAsync(
                        pf => pf.ProfileId == collection.ProfileId && pf.FollowerId == currentUser.Profile.Id);
                }
                if (!isOwner && !isFollower)
                {
                    return null;
                }
            }

            var collectionItems = await _dbContext.Set<BookmarkCollectionItem>()
                .Where(bci => bci.BookmarkCollectionId == collection.Id)
                .Include(bci => bci.Post)
                    .ThenInclude(p => p.User)
                        .ThenInclude(u => u.Profile)
                .Include(bci => bci.Post)
                    .ThenInclude(p => p.MediaFile)
                .Include(bci => bci.Post)
                    .ThenInclude(p => p.BookmarkCollectionItems)
                        .ThenInclude(bci2 => bci2.BookmarkCollection)
                .ToListAsync();

            return new BookmarkCollectionResponse
            {
                Uid = collection.Uid,
                Name = collection.Name,
                PostsCount = collectionItems.Count(bci => bci.Post != null && bci.Post.IsActive),
                Items = collectionItems
                    .Where(bci => bci.Post != null && bci.Post.IsActive)
                    .Select(bci => {
                        var postResponse = _mapper.Map<PostResponse>(bci.Post);
                        // Set ProfileUid to the post owner's profile UID if not a store
                        if (bci.Post.Store == null && bci.Post.User != null && bci.Post.User.Profile != null)
                        {
                            postResponse.ProfileUid = bci.Post.User.Profile.Uid;
                        }
                        // Set bookmark status for current user (check collections only)
                        var currentUser = _currentUserService.GetUserAsync().Result;
                        if (currentUser?.Profile != null)
                        {
                            postResponse.BookmarkedByMe = bci.Post.BookmarkCollectionItems?.Any(bci2 => bci2.BookmarkCollection.ProfileId == currentUser.Profile.Id && bci2.BookmarkCollection.IsActive) ?? false;
                        }
                        return postResponse;
                    })
                    .ToList(),
                PreviewImages = collectionItems
                    .Where(bci => bci.Post != null && bci.Post.IsActive && bci.Post.MediaFile != null)
                    .Take(4)
                    .Select(bci => string.IsNullOrEmpty(bci.Post.ThumbnailUrl) ? (bci.Post.MediaFile.OriginalUrl ?? bci.Post.MediaFile.Url) : bci.Post.ThumbnailUrl)
                    .ToList(),
                PostUids = collectionItems
                    .Where(bci => bci.Post != null && bci.Post.IsActive)
                    .Select(bci => bci.Post.Uid)
                    .ToList()
            };
        }

        public async Task ShareCollectionWithUserAsync(string collectionUid, string senderProfileUid, string targetProfileUid)
        {
            var sender = await _dbContext.Profiles.Include(p => p.User).FirstOrDefaultAsync(p => p.Uid == senderProfileUid);
            var target = await _dbContext.Profiles.Include(p => p.User).FirstOrDefaultAsync(p => p.Uid == targetProfileUid);
            if (sender == null || target == null) throw new KeyNotFoundException("User not found");

            var message = $"{sender.User.UserName} shared a collection with you!";

            // Send push notification with collectionUid in data payload
            await _notificationService.SaveCollectionShareNotificationAsync(
                sender.User.Id,
                target.User.Id,
                collectionUid,
                message
                //data: new System.Collections.Generic.Dictionary<string, string> { { "collectionUid", collectionUid } }
            );
        }

        // public async Task<List<BookmarkCollectionResponse>> GetAllCollectionsWithPostsAsync(string profileUid)
        // {
        //     var profile = await _dbContext.Profiles.SingleOrDefaultAsync(p => p.Uid == profileUid);

        //     if(profile == null)
        //     {
        //         // Validate profileUid using OptimizedValidationBase for consistent validation
        //         //ValidateProfileUid(profileUid);

        //        profile = await _dbContext.Profiles
        //             .Include(p => p.User)
        //             .FirstOrDefaultAsync(p => p.User.UserName == profileUid);
        //     }

        //     // If profile not found, return empty list
        //     if (profile == null) return new List<BookmarkCollectionResponse>();

        //      // Privacy: enforce visibility for private profiles (only owner/followers can see)
        //     var isPrivate = await _dbContext.ProfileSettings
        //         .Where(ps => ps.ProfileId == profile.Id)
        //         .Select(ps => !ps.IsProfilePublic)
        //         .FirstOrDefaultAsync();

        //     if (isPrivate)
        //     {
        //         var currentUser = await _currentUserService.GetUserAsync();
        //         var isOwner = currentUser?.Profile?.Id == profile.Id;
        //         var isFollower = false;
        //         if (!isOwner && currentUser?.Profile != null)
        //         {
        //             isFollower = await _dbContext.ProfileFollowers.AnyAsync(
        //                 pf => pf.ProfileId == profile.Id && pf.FollowerId == currentUser.Profile.Id);
        //         }
        //         if (!isOwner && !isFollower)
        //         {
        //             return new List<BookmarkCollectionResponse>();
        //         }
        //     }

        //     // Ensure user has a "Saved" collection - create one if it doesn't exist
        //     await EnsureSavedCollectionExistsAsync(profile);

        //     var result = new List<BookmarkCollectionResponse>();

        //     // Get all collections for this profile, including the "Saved" collection
        //     var collections = await _dbContext.BookmarkCollections
        //         .Where(c => c.ProfileId == profile.Id)
        //         .ToListAsync();

        //     // Separate "Saved" collection from other collections
        //     var savedCollection = collections.FirstOrDefault(c => c.Name == "Saved");
        //     var otherCollections = collections.Where(c => c.Name != "Saved").ToList();

        //     // Process "Saved" collection first (if it exists)
        //     if (savedCollection != null)
        //     {
        //         var savedCollectionItems = await _dbContext.Set<BookmarkCollectionItem>()
        //             .Where(bci => bci.BookmarkCollectionId == savedCollection.Id)
        //             .Include(bci => bci.Post)
        //                 .ThenInclude(p => p.User)
        //                     .ThenInclude(u => u.Profile)
        //             .Include(bci => bci.Post)
        //                 .ThenInclude(p => p.MediaFile)
        //             .Include(bci => bci.Post)
        //                 .ThenInclude(p => p.BookmarkCollectionItems)
        //                     .ThenInclude(bci2 => bci2.BookmarkCollection)
        //             .ToListAsync();

        //         var savedCollectionResponse = new BookmarkCollectionResponse
        //         {
        //             Uid = savedCollection.Uid,
        //             Name = savedCollection.Name,
        //             PostsCount = savedCollectionItems.Count(bci => bci.Post != null && bci.Post.IsActive),
        //             Items = savedCollectionItems
        //                 .Where(bci => bci.Post != null && bci.Post.IsActive)
        //                 .Select(bci => {
        //                     var postResponse = _mapper.Map<PostResponse>(bci.Post);
        //                     // Set ProfileUid to the post owner's profile UID if not a store
        //                     if (bci.Post.User != null && bci.Post.User.Profile != null)
        //                     {
        //                         postResponse.ProfileUid = bci.Post.User.Profile.Uid;
        //                     }
        //                     // Set bookmark status for current user
        //                     var currentUser = _currentUserService.GetUserAsync().Result;
        //                     if (currentUser?.Profile != null)
        //                     {
        //                     }
        //                     return postResponse;
        //                 })
        //                 .ToList(),
        //             PreviewImages = savedCollectionItems
        //                 .Where(bci => bci.Post != null && bci.Post.IsActive && bci.Post.MediaFile != null)
        //                 .Take(4)
        //                 .Select(bci => bci.Post.MediaFile.Url)
        //                 .ToList(),
        //             PostUids = savedCollectionItems
        //                 .Where(bci => bci.Post != null && bci.Post.IsActive)
        //                 .Select(bci => bci.Post.Uid)
        //                 .ToList()
        //         };

        //         result.Add(savedCollectionResponse);
        //     }

        //     // Sort other collections by the most recently added post (BookmarkCollectionItem.CreatedAt) in each collection, descending
        //     var collectionsWithLatest = otherCollections
        //         .Select(collection => new {
        //             Collection = collection,
        //             LatestAdded = _dbContext.Set<BookmarkCollectionItem>()
        //                 .Where(bci => bci.BookmarkCollectionId == collection.Id)
        //                 .OrderByDescending(bci => bci.CreatedAt)
        //                 .Select(bci => bci.CreatedAt)
        //                 .FirstOrDefault()
        //         })
        //         .OrderByDescending(x => x.LatestAdded)
        //         .ToList();

        //     foreach (var entry in collectionsWithLatest)
        //     {
        //         var collection = entry.Collection;
        //         var collectionItems = await _dbContext.Set<BookmarkCollectionItem>()
        //             .Where(bci => bci.BookmarkCollectionId == collection.Id)
        //             .Include(bci => bci.Post)
        //                 .ThenInclude(p => p.User)
        //                     .ThenInclude(u => u.Profile)
        //             .Include(bci => bci.Post)
        //                 .ThenInclude(p => p.MediaFile)
        //             .Include(bci => bci.Post)
        //                 .ThenInclude(p => p.BookmarkCollectionItems)
        //                     .ThenInclude(bci2 => bci2.BookmarkCollection)
        //             .ToListAsync();

        //         result.Add(new BookmarkCollectionResponse
        //         {
        //             Uid = collection.Uid,
        //             Name = collection.Name,
        //             PostsCount = collectionItems.Count(bci => bci.Post != null && bci.Post.IsActive),
        //             Items = collectionItems
        //                 .Where(bci => bci.Post != null && bci.Post.IsActive)
        //                 .Select(bci => {
        //                     var postResponse = _mapper.Map<PostResponse>(bci.Post);
        //                     // Set ProfileUid to the post owner's profile UID if not a store
        //                     if (bci.Post.User != null && bci.Post.User.Profile != null)
        //                     {
        //                         postResponse.ProfileUid = bci.Post.User.Profile.Uid;
        //                     }
        //                     // Set bookmark status for current user (check both bookmarks and collections)
        //                     var currentUser = _currentUserService.GetUserAsync().Result;
        //                     if (currentUser?.Profile != null)
        //                     {
        //                     }
        //                     return postResponse;
        //                 })
        //                 .ToList(),
        //             PreviewImages = collectionItems
        //                 .Where(bci => bci.Post != null && bci.Post.IsActive && bci.Post.MediaFile != null)
        //                 .Take(4)
        //                 .Select(bci => bci.Post.MediaFile.Url)
        //                 .ToList(),
        //             PostUids = collectionItems
        //                 .Where(bci => bci.Post != null && bci.Post.IsActive)
        //                 .Select(bci => bci.Post.Uid)
        //                 .ToList()
        //         });
        //     }
        //     return result;
        // }

        public async Task<List<BookmarkCollectionResponse>> GetAllCollectionsWithPostsAsync(string profileUid)
        {
            // Step 1: Get current user ONCE at the beginning
            var currentUser = await _currentUserService.GetUserAsync();
            var currentProfileId = currentUser?.Profile?.Id;

            // Step 2: Find profile (optimized - single query)
            var profile = await _dbContext.Profiles
                    .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Uid == profileUid || p.User.UserName == profileUid);

            if (profile == null)
                return new List<BookmarkCollectionResponse>();

            // Step 3: Privacy check (optimized)
            var isPrivate = await _dbContext.ProfileSettings
                .Where(ps => ps.ProfileId == profile.Id)
                .Select(ps => !ps.IsProfilePublic)
                .FirstOrDefaultAsync();

            if (isPrivate)
            {
                var isOwner = currentProfileId == profile.Id;

                if (!isOwner && currentProfileId.HasValue)
                {
                    var isFollower = await _dbContext.ProfileFollowers
                        .AnyAsync(pf => pf.ProfileId == profile.Id && pf.FollowerId == currentProfileId.Value);

                    if (!isFollower)
                        return new List<BookmarkCollectionResponse>();
                }
                else if (!isOwner)
                {
                    return new List<BookmarkCollectionResponse>();
                }
            }

            // Step 4: Ensure "Saved" collection exists
            await EnsureSavedCollectionExistsAsync(profile);

            // Step 5: Get ALL collections with items in ONE query
            var collectionsWithItems = await _dbContext.BookmarkCollections
                .Where(c => c.ProfileId == profile.Id && c.IsActive)
                .Include(c => c.BookmarkCollectionItems
                    .Where(bci => bci.Post != null && bci.Post.IsActive))
                    .ThenInclude(bci => bci.Post)
                    .ThenInclude(p => p.User)
                        .ThenInclude(u => u.Profile)
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                    .ThenInclude(p => p.MediaFile)
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                .Include(c => c.BookmarkCollectionItems)
                    .ThenInclude(bci => bci.Post)
                    .ThenInclude(p => p.BookmarkCollectionItems)
                        .ThenInclude(bci2 => bci2.BookmarkCollection)
                .ToListAsync(); // ← SINGLE DATABASE QUERY!

            // ========================================
            // LOCAL FUNCTION - Mapping Logic
            // ========================================
            BookmarkCollectionResponse MapCollection(BookmarkCollection collection)
            {
                var activeItems = collection.BookmarkCollectionItems
                    .Where(bci => bci.Post != null && bci.Post.IsActive)
                .ToList();

                return new BookmarkCollectionResponse
                {
                    Uid = collection.Uid,
                    Name = collection.Name,
                    PostsCount = activeItems.Count,

                    Items = activeItems.Select(bci =>
                    {
                        // Use AutoMapper for the base mapping
                            var postResponse = _mapper.Map<PostResponse>(bci.Post);

                        // Set ProfileUid to the post owner's profile UID
                        if (bci.Post.User?.Profile != null)
                            {
                                postResponse.ProfileUid = bci.Post.User.Profile.Uid;
                            }

                        // Set bookmark status for current user
                        if (currentProfileId.HasValue)
                        {
                            postResponse.BookmarkedByMe =
                                (bci.Post.BookmarkCollectionItems?.Any(bci2 => bci2.BookmarkCollection.ProfileId == currentProfileId.Value && bci2.BookmarkCollection.IsActive) ?? false);
                        }

                            return postResponse;
                    }).ToList(),

                    PreviewImages = activeItems
                        .Where(bci => bci.Post.MediaFile != null)
                        .Take(4)
                        .Select(bci => string.IsNullOrEmpty(bci.Post.ThumbnailUrl) ? (bci.Post.MediaFile.OriginalUrl ?? bci.Post.MediaFile.Url) : bci.Post.ThumbnailUrl)
                        .ToList(),

                    PostUids = activeItems
                        .Select(bci => bci.Post.Uid)
                        .ToList()
                };
            }
            // ========================================
            // END LOCAL FUNCTION
            // ========================================

            // Step 6: Process results in memory
            var result = new List<BookmarkCollectionResponse>();

            // Separate "Saved" collection from others
            var savedCollection = collectionsWithItems.FirstOrDefault(c => c.Name.ToLower() == "saved");
            var otherCollections = collectionsWithItems.Where(c => c.Name.ToLower() != "saved").ToList();

            // Process "Saved" collection first
            if (savedCollection != null)
            {
                result.Add(MapCollection(savedCollection));
            }

            // Sort other collections by latest item date
            var sortedCollections = otherCollections
                .Select(c => new
                {
                    Collection = c,
                    LatestDate = c.BookmarkCollectionItems
                        .Where(bci => bci.Post != null && bci.Post.IsActive)
                        .OrderByDescending(bci => bci.CreatedAt)
                        .Select(bci => bci.CreatedAt)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.LatestDate)
                .Select(x => x.Collection)
                .ToList();

            // Process other collections
            foreach (var collection in sortedCollections)
            {
                result.Add(MapCollection(collection));
            }

            return result;
        }
        public async Task<PostResponse> GetPostByUidAsync(string postUid)
        {
            var post = await _dbContext.Posts
                .Include(p => p.MediaFile)
                .FirstOrDefaultAsync(p => p.Uid == postUid);
            if (post == null) return null;
            return _mapper.Map<PostResponse>(post);
        }

        /// <summary>
        /// Ensures that a user has a "Saved" collection. Creates one if it doesn't exist.
        /// This method handles both new users and existing users who don't have a "Saved" collection.
        /// </summary>
        private async Task EnsureSavedCollectionExistsAsync(Domain.Entities.Profile profile)
        {
            // Check if user already has a "Saved" collection
            var existingSavedCollection = await _dbContext.BookmarkCollections
                .FirstOrDefaultAsync(c => c.ProfileId == profile.Id && c.Name.ToLower() == "saved");

            if (existingSavedCollection == null)
            {
                // Create the default "Saved" collection for existing users
                var defaultCollection = new BookmarkCollection
                {
                    Name = "Saved",
                    ProfileId = profile.Id,
                    ProfileUid = profile.Uid
                };

                _dbContext.BookmarkCollections.Add(defaultCollection);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
            else if (!existingSavedCollection.IsActive)
            {
                // Reactivate the existing "Saved" collection if it was deactivated
                existingSavedCollection.IsActive = true;
                await _dbContext.SaveChangesAsync(CancellationToken.None);
            }
        }
    }
} 