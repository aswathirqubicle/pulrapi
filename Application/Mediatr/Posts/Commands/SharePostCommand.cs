using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Post;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class SharePostCommand : IRequest<PostResponse>
    {
        [Required]
        public string SharedPostUid { get; set; }
    }

    public class SharePostCommandHandler : IRequestHandler<SharePostCommand, PostResponse>
    {
        private readonly ILogger<SharePostCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public SharePostCommandHandler(ILogger<SharePostCommandHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<PostResponse> Handle(SharePostCommand request, CancellationToken cancellationToken)
        {
            // SharePostDto model = _mapper.Map<SharePostDto>(request);
            try
            {
                var postToShare = await _dbContext.Posts
                    .AsSplitQuery()
                    .Include(p => p.PostProfileMentions).ThenInclude(pp => pp.Profile).ThenInclude(p => p.User)
                    .Include(p => p.PostStoreMentions).ThenInclude(pp => pp.Store)
                    .Include(p => p.PostProductTags).ThenInclude(pp => pp.Product).ThenInclude(pr => pr.Store)
                    .Include(p => p.PostHashtags).ThenInclude(pp => pp.Hashtag)
                    .Include(p => p.Store)
                    .Include(p => p.MediaFile)
                    .SingleOrDefaultAsync(p => p.Uid == request.SharedPostUid && p.IsActive, cancellationToken);
                if (postToShare == null)
                {
                    throw new BadRequestException("Post that you're trying to share doesn't exist");
                }

                var cUser = await _currentUserService.GetUserAsync();

                var taggedProducts = new List<Product>();
                if (postToShare.PostProductTags.Count > 0)
                {
                    var productUids = postToShare.PostProductTags.Select(e => e.Product.Uid).ToList();
                    taggedProducts = await _dbContext.Products.Where(p => productUids.Contains(p.Uid) && p.IsActive).ToListAsync(CancellationToken.None);
                }

                var mentionedProfiles = await _dbContext.Profiles.Include(u => u.User).Where(e => postToShare.PostProfileMentions
                    .Select(p => p.Profile.User.UserName).Contains(e.User.UserName)).ToListAsync(cancellationToken);

                var mentionedStores = await _dbContext.Stores.Where(e => postToShare.PostStoreMentions.Select(ps => ps.Store.UniqueName).Contains(e.UniqueName))
                    .ToListAsync(cancellationToken);

                var existingHashtags = await _dbContext.Hashtags.Where(ht => postToShare.PostHashtags.Select(ph => ph.Hashtag.Value).Contains(ht.Value))
                    .ToListAsync(cancellationToken);
                var hashtagsWithoutDuplicates = postToShare.PostHashtags.Select(ph => ph.Hashtag.Value)
                    .Where(value => !existingHashtags.Select(eh => eh.Value.Trim().ToLower()).Contains(value.Trim().ToLower())).ToList();

                //MediaFile existingMediaFile = null;

                /*if (postToShare.MediaFile != null)
                {
                    existingMediaFile = await _dbContext.MediaFiles.SingleOrDefaultAsync(mf => mf.Uid == request.MediaFileUid, cancellationToken);
                    if (existingMediaFile == null)
                    {
                        throw new NotFoundException("Media file not found");
                    }
                }*/

                var newPost = new Post
                {
                    Text = postToShare.Text,
                    User = cUser,
                    Store = postToShare.Store != null
                        ? await _dbContext.Stores.SingleOrDefaultAsync(s => s.UserId == cUser.Id && s.Uid == postToShare.Store.Uid, cancellationToken)
                        : null,
                    MediaFile = postToShare.MediaFile,
                    SharedPost = postToShare,
                    PostProfileMentions = mentionedProfiles.ConvertAll(e => new PostProfileMention { Profile = e }).ToList(),
                    PostStoreMentions = mentionedStores.ConvertAll(e => new PostStoreMention { Store = e }).ToList(),
                    PostProductTags = postToShare.PostProductTags.Select(e => new PostProductTag
                    {
                        PositionLeftPercent = e.PositionLeftPercent,
                        PositionTopPercent = e.PositionTopPercent,
                        Product = taggedProducts.SingleOrDefault(p => p.Uid == e.Product.Uid)
                    }).ToList()
                };

                newPost.PostHashtags = hashtagsWithoutDuplicates.Select(val => new PostHashtag { Hashtag = new Hashtag { Value = val } }).ToList();
                if (existingHashtags.Any())
                {
                    foreach (var existingHashtag in existingHashtags)
                    {
                        newPost.PostHashtags.Add(new PostHashtag { Hashtag = existingHashtag });
                    }
                }

                _dbContext.Posts.Add(newPost);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                var x = new PostResponse
                {
                    Uid = newPost.Uid,
                    //StoreUid = newPost.Store?.Uid,
                    ProfileUid = newPost.Store == null ? cUser.Profile.Uid : null,
                    Text = newPost.Text,
                    MediaFile = _mapper.Map<MediaFileDetailsResponse>(newPost.MediaFile),
                    LikesCount = newPost.PostLikes.Count(),
                    LikedByMe = cUser != null && newPost.PostLikes.Any(pl => pl.LikedById == cUser.Profile.Id),
                    TaggedProductUids = newPost.PostProductTags.Where(ppt => ppt.Product != null && ppt.Product.IsActive).Select(ppt => ppt.Product.Uid),
                    CreatedAt = newPost.CreatedAt,
                    //PostedByStore = newPost.Store != null,
                    PostProfileMentions = newPost.PostProfileMentions.Select(e => new TaggedUserResponse
                    {
                        ProfileUid = e.Profile.Uid,
                        Username = e.Profile.User.UserName,
                        FirstName = e.Profile.User.FirstName,
                        ProfileImageUrl = e.Profile.ImageUrl,
                        FollowedByMe = cUser != null && e.Profile.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id),
                        UserType = e.Profile.UserType
                    }).ToList(),
                    //PostStoreMentions = newPost.PostStoreMentions.Select(e => e.Store.UniqueName).ToList(),
                    BookmarkedByMe = cUser != null && newPost.BookmarkCollectionItems.Any(bci => bci.BookmarkCollection.ProfileId == cUser.Profile.Id && bci.BookmarkCollection.IsActive),
                    BookmarksCount = newPost.BookmarkCollectionItems.Where(bci => bci.BookmarkCollection.IsActive).Select(bci => bci.BookmarkCollection.ProfileId).Distinct().Count(),
                    MyStylesCount = newPost.PostMyStyles.Count,
                    //Store = newPost.Store != null
                    //    ? new StoreBaseResponse()
                    //    {
                    //        Uid = newPost.Store.Uid,
                    //        Name = newPost.Store.Name,
                    //        ImageUrl = newPost.Store.ImageUrl,
                    //        UniqueName = newPost.Store.UniqueName,
                    //        CurrencyCode = newPost.Store.Currency.Code,
                    //        FollowedByMe = cUser != null && newPost.Store.StoreFollowers.Any(sf => sf.FollowerId == cUser.Profile.Id),
                    //    }
                    //    : null,
                    PostProductTags = newPost.PostProductTags.Where(e => e.Product != null && e.Product.IsActive).Select(e => new PostProductTagResponse()
                    {
                        Product = new ProductPublicResponse()
                        {
                            Uid = e.Product.Uid,
                            Name = e.Product.Name,
                            Price = e.Product.MinPrice,
                            CurrencyCode = e.Product.Store.Currency?.Code,
                            StoreName = e.Product.Store.Name,
                            CountryCode = e.Product.Country != null ? e.Product.Country.Iso2 : null,
                            ProductMediaFiles = e.Product.ProductMediaFiles
                                .Where(pmf => pmf.MediaFile.IsActive)
                                .Select(pmf => new MediaFileDetailsResponse
                                {
                                    Uid = pmf.MediaFile.Uid,
                                    Url = pmf.MediaFile.Url,
                                    FileType = pmf.MediaFile.MediaFileType.ToString()
                                }).ToList()
                        },
                        PositionLeftPercent = e.PositionLeftPercent,
                        PositionTopPercent = e.PositionTopPercent
                    }).ToList(),
                    Profile = newPost.Store == null
                        ? new ProfileBaseResponse()
                        {
                            Uid = cUser.Profile.User.Profile.Uid,
                            FullName = cUser.Profile.User.FirstName,
                            FirstName = cUser.Profile.User.FirstName,
                            LastName = cUser.Profile.User.LastName,
                            ImageUrl = cUser.Profile.User.Profile.ImageUrl,
                            Username = cUser.Profile.User.UserName,
                            FollowedByMe = false,
                        }
                        : null,
                    CommentsCount = newPost.Comments.Count,
                    PostType = PostTypeEnum.Feed,
                };

                return x;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}