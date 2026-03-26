using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models.Post;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class ReplacePostProductTagsCommand : IRequest<PostDetailsResponse>
    {
        public string PostUid { get; set; } // Public property for controller use
        public List<PostProductTagDto> PostProductTags { get; set; } = new List<PostProductTagDto>();
    }

    public class ReplacePostProductTagsCommandHandler : IRequestHandler<ReplacePostProductTagsCommand, PostDetailsResponse>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<ReplacePostProductTagsCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public ReplacePostProductTagsCommandHandler(
            IApplicationDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            ILogger<ReplacePostProductTagsCommandHandler> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<PostDetailsResponse> Handle(ReplacePostProductTagsCommand request, CancellationToken cancellationToken)
        {
            var currentUser = await _currentUserService.GetUserAsync();
            if (currentUser == null)
            {
                throw new NotAuthenticatedException("User not authenticated");
            }

            // Get the post from the URL parameter (passed from controller)
            var postUid = request.PostUid; // This will come from controller parameter
            var post = await _dbContext.Posts
                .Include(p => p.User)
                .Include(p => p.PostProductTags)
                    .ThenInclude(pt => pt.Product)
                        .ThenInclude(p => p.User)
                            .ThenInclude(u => u.Profile)
                .Include(p => p.PostProductTags)
                    .ThenInclude(pt => pt.Product)
                        .ThenInclude(p => p.ProductMediaFiles)
                            .ThenInclude(pmf => pmf.MediaFile)
                .Include(p => p.PostProductTags)
                    .ThenInclude(pt => pt.Product)
                        .ThenInclude(p => p.Store)
                .FirstOrDefaultAsync(p => p.Uid == postUid, cancellationToken);

            if (post == null)
            {
                throw new NotFoundException($"Post with UID {postUid} not found");
            }

            // Check if the current user owns this post
            if (post.User.Id != currentUser.Id)
            {
                throw new ForbiddenException("You can only edit your own posts");
            }

            // Remove existing product tags
            _dbContext.PostProductTags.RemoveRange(post.PostProductTags);

            // Add new product tags
            if (request.PostProductTags.Count > 0)
            {
                // Get product IDs from UIDs
                var productUids = request.PostProductTags.Select(pt => pt.ProductUid).ToList();
                
                // Debug: Log the product UIDs we're looking for
                _logger.LogInformation($"Looking for products with UIDs: {string.Join(", ", productUids)}");
                
                // Debug: Check if products exist (without IsActive filter first)
                var allProducts = await _dbContext.Products
                    .Where(p => productUids.Contains(p.Uid))
                    .Select(p => new { p.Uid, p.IsActive, p.Name })
                    .ToListAsync(cancellationToken);
                
                _logger.LogInformation($"Found {allProducts.Count} products without IsActive filter:");
                foreach (var p in allProducts)
                {
                    _logger.LogInformation($"  UID: {p.Uid}, IsActive: {p.IsActive}, Name: {p.Name}");
                }
                
                // Now get the active products
                var products = await _dbContext.Products
                    .Where(p => productUids.Contains(p.Uid) && p.IsActive)
                    .ToDictionaryAsync(p => p.Uid, p => p.Id, cancellationToken);
                
                _logger.LogInformation($"Found {products.Count} active products");
                
                var tagThumbnailUids = request.PostProductTags
                    .Where(e => !string.IsNullOrWhiteSpace(e.ThumbnailUid))
                    .Select(e => e.ThumbnailUid)
                    .Distinct()
                    .ToList();
                var tagThumbnails = await _dbContext.MediaFiles
                    .Where(mf => tagThumbnailUids.Contains(mf.Uid))
                    .ToDictionaryAsync(mf => mf.Uid, mf => mf.Url, cancellationToken);

                var newProductTags = request.PostProductTags.Select(e => 
                {
                    if (!products.ContainsKey(e.ProductUid))
                    {
                        throw new BadRequestException($"Product with UID {e.ProductUid} not found or is inactive.");
                    }

                    // Convert pixel coordinates to percentages, using post dimensions as fallback if per-tag dimensions are zero
                    var effectiveWidth = e.ImageWidth > 0 ? e.ImageWidth : (post.ImageWidth ?? 0);
                    var effectiveHeight = e.ImageHeight > 0 ? e.ImageHeight : (post.ImageHeight ?? 0);

                    var leftPercent = effectiveWidth > 0 ? (e.LocationX / effectiveWidth) * 100 : 0;
                    var topPercent = effectiveHeight > 0 ? (e.LocationY / effectiveHeight) * 100 : 0;
                    
                    // Ensure percentages are within valid range
                    leftPercent = Math.Max(0, Math.Min(100, leftPercent));
                    topPercent = Math.Max(0, Math.Min(100, topPercent));
                    
                    return new PostProductTag
                    {
                        PostId = post.Id,
                        ProductId = products[e.ProductUid],
                        PositionLeftPercent = leftPercent,
                        PositionTopPercent = topPercent,
                        LocationX = e.LocationX,
                        LocationY = e.LocationY,
                        CreatedAt = DateTime.UtcNow,
                        ThumbnailUrl = !string.IsNullOrWhiteSpace(e.ThumbnailUid) && tagThumbnails.ContainsKey(e.ThumbnailUid) 
                            ? tagThumbnails[e.ThumbnailUid] 
                            : null
                    };
                }).ToList();

                await _dbContext.PostProductTags.AddRangeAsync(newProductTags, cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Return updated post
            var postResponse = await _dbContext.Posts
                .Include(p => p.User)
                .Include(p => p.PostProductTags)
                    .ThenInclude(pt => pt.Product)
                        .ThenInclude(p => p.User)
                            .ThenInclude(u => u.Profile)
                .Include(p => p.PostProductTags)
                    .ThenInclude(pt => pt.Product)
                        .ThenInclude(p => p.ProductMediaFiles)
                            .ThenInclude(pmf => pmf.MediaFile)
                .Include(p => p.PostProductTags)
                    .ThenInclude(pt => pt.Product)
                        .ThenInclude(p => p.Store)
                .FirstOrDefaultAsync(p => p.Uid == postUid, cancellationToken);

            return _mapper.Map<PostDetailsResponse>(postResponse);
        }
    }
}
