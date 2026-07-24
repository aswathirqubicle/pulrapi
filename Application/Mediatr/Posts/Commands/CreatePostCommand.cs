using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models.Post;
using Core.Application.Services;
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
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Core.Application.Models;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class CreatePostCommand : IRequest<PostDetailsResponse>
    {
        //public string StoreUid { get; set; }
        public string Text { get; set; }
        public List<string> Hashtags { get; set; } = new List<string>();
        public List<string> Mentions { get; set; } = new List<string>();
        public double SpotExpiryHours { get; set; } = 0;
        public List<PostProductTagDto> PostProductTags { get; set; } = new List<PostProductTagDto>();
        [Required]
        public string MediaFileUid { get; set; }
        public string Location { get; set; }
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public int? VideoWidth { get; set; }
        public int? VideoHeight { get; set; }
        public string ThumbnailUid { get; set; }
        public string? CollabId { get; set; }
    }

    public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, PostDetailsResponse>
    {
        private readonly ILogger<CreatePostCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;
        private readonly IFileUploadService _fileUploadService;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;

        public CreatePostCommandHandler(ILogger<CreatePostCommandHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IApplicationDbContext dbContext, IMediator mediator, INotificationService notificationService, IFileUploadService fileUploadService, IConfiguration configuration)
        {
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _mediator = mediator;
            _notificationService = notificationService;
            _fileUploadService = fileUploadService;
            _configuration = configuration;
        }

        public async Task<PostDetailsResponse> Handle(CreatePostCommand request, CancellationToken cancellationToken)
        {
            CreatePostDto model = _mapper.Map<CreatePostDto>(request);
            try
            {
                var user = await _currentUserService.GetUserAsync(true);
                
                var tagThumbnailUids = model.PostProductTags
                    .Where(e => !string.IsNullOrWhiteSpace(e.ThumbnailUid))
                    .Select(e => e.ThumbnailUid)
                    .Distinct()
                    .ToList();
                var tagThumbnails = await _dbContext.MediaFiles
                    .Where(mf => tagThumbnailUids.Contains(mf.Uid))
                    .ToDictionaryAsync(mf => mf.Uid, mf => mf.Url, cancellationToken);

                var taggedProducts = new List<Product>();
                if (model.PostProductTags.Count > 0)
                {
                    var productUids = model.PostProductTags.Select(e => e.ProductUid).ToList();
                    taggedProducts = await _dbContext.Products.Include(p => p.Country).Where(p => productUids.Contains(p.Uid) && p.IsActive).ToListAsync(CancellationToken.None);
                };

                // Strip '@' prefix from mentions if present
                var normalizedMentions = model.Mentions
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Select(m => m.StartsWith("@") ? m.Substring(1) : m)
                    .ToList();

                var mentionedProfiles = await _dbContext.Profiles.Include(u => u.User).Where(e => normalizedMentions.Contains(e.User.UserName)).ToListAsync();
                //var mentionedStores = await _dbContext.Stores.Where(e => model.Mentions.Contains(e.UniqueName)).ToListAsync(cancellationToken);

                var existingHashtags = await _dbContext.Hashtags.Where(ht => model.Hashtags.Contains(ht.Value)).ToListAsync(cancellationToken);
                var hashtagsWithoutDuplicates = model.Hashtags
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Where(value => !existingHashtags.Select(eh => eh.Value.Trim().ToLower()).Contains(value.Trim().ToLower()))
                    .ToList();

                var existingMediaFile = await _dbContext.MediaFiles.SingleOrDefaultAsync(mf => mf.Uid == request.MediaFileUid, cancellationToken);
                if(existingMediaFile == null) {
                    throw new NotFoundException("Media file not found");
                }

                string thumbnailUrl = null;
                if (!string.IsNullOrWhiteSpace(request.ThumbnailUid))
                {
                    var thumbnailMediaFile = await _dbContext.MediaFiles.SingleOrDefaultAsync(mf => mf.Uid == request.ThumbnailUid, cancellationToken);
                    if (thumbnailMediaFile != null)
                    {
                        thumbnailUrl = thumbnailMediaFile.Url;
                    }
                    else
                    {
                        _logger.LogWarning("Thumbnail media file with UID {Uid} not found", request.ThumbnailUid);
                    }
                }

                var newPost = new Post
                {
                    Text = model.Text,
                    ImgDescription = model.ImgDescription,
                    User = user,
                    Store = model.StoreUid != null ? await _dbContext.Stores.SingleOrDefaultAsync(s => s.UserId == user.Id && s.Uid == model.StoreUid, cancellationToken) : null,
                    MediaFile = existingMediaFile,
                    CollabId = request.CollabId,
                    PostProfileMentions = mentionedProfiles.ConvertAll(e => new PostProfileMention { Profile = e }).ToList(),
                    //PostStoreMentions = mentionedStores.ConvertAll(e => new PostStoreMention { Store = e }).ToList(),
                    PostProductTags = model.PostProductTags.Select(e => 
                    {
                        var product = taggedProducts.SingleOrDefault(p => p.Uid == e.ProductUid);
                        if (product == null)
                        {
                            throw new BadRequestException($"Product with UID {e.ProductUid} not found or is inactive.");
                        }

                        // Convert pixel coordinates to percentages, using top-level dimensions as fallback if per-tag dimensions are zero
                        var effectiveWidth = e.ImageWidth > 0 ? e.ImageWidth : (request.ImageWidth ?? request.VideoWidth ?? 0);
                        var effectiveHeight = e.ImageHeight > 0 ? e.ImageHeight : (request.ImageHeight ?? request.VideoHeight ?? 0);

                        var leftPercent = effectiveWidth > 0 ? (e.LocationX / effectiveWidth) * 100 : 0;
                        var topPercent = effectiveHeight > 0 ? (e.LocationY / effectiveHeight) * 100 : 0;
                        
                        // Ensure percentages are within valid range
                        leftPercent = Math.Max(0, Math.Min(100, leftPercent));
                        topPercent = Math.Max(0, Math.Min(100, topPercent));
                        
                        return new PostProductTag
                        {
                            PositionLeftPercent = leftPercent,
                            PositionTopPercent = topPercent,
                            LocationX = e.LocationX,
                            LocationY = e.LocationY,
                            ProductId = product.Id,
                            Product = product,
                            ThumbnailUrl = !string.IsNullOrWhiteSpace(e.ThumbnailUid) && tagThumbnails.ContainsKey(e.ThumbnailUid) 
                                ? tagThumbnails[e.ThumbnailUid] 
                                : null
                        };
                    }).ToList(),
                    Location = model.Location,
                    ImageWidth = request.ImageWidth,
                    ImageHeight = request.ImageHeight,
                    VideoWidth = request.VideoWidth,
                    VideoHeight = request.VideoHeight,
                    ThumbnailUrl = thumbnailUrl
                };

                // Create new hashtags
                var newHashtags = hashtagsWithoutDuplicates.Select(val => new Hashtag { Value = val.Trim() }).ToList();
                if (newHashtags.Any())
                {
                    await _dbContext.Hashtags.AddRangeAsync(newHashtags, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                // Create post hashtag relationships
                newPost.PostHashtags = newHashtags.Select(h => new PostHashtag { Hashtag = h }).ToList();
                if (existingHashtags.Any())
                {
                    foreach (var existingHashtag in existingHashtags)
                    {
                        newPost.PostHashtags.Add(new PostHashtag { Hashtag = existingHashtag });
                    }
                }

                _dbContext.Posts.Add(newPost);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                
                // Send mention notifications to tagged users
                foreach (var mentionedProfile in mentionedProfiles)
                {
                    // Don't send notification if user mentions themselves
                    if (mentionedProfile.UserId != user.Id)
                    {
                        await _notificationService.SaveMentionNotificationAsync(
                            user.Id, 
                            mentionedProfile.UserId, 
                            newPost.Uid, 
                            "Post"
                        );
                    }
                }
                
                return await _mediator.Send(new GetPostQuery() { Uid = newPost.Uid });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
