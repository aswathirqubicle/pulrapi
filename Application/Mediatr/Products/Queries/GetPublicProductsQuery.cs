using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Models;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetPublicProductsQuery : PagingParamsRequest, IRequest<PagingResponse<ProductPublicResponse>>
    {
        public string Username { get; set; }
        public ProductTypeEnum? Type { get; set; }
        public string? CollabId { get; set; }
    }

    public class GetPublicProductsQueryHandler : IRequestHandler<GetPublicProductsQuery, PagingResponse<ProductPublicResponse>>
    {
        private readonly ILogger<GetPublicProductsQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IQueryHelperService _queryHelperService;
        private readonly IExchangeRateService _exchangeRateService;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public GetPublicProductsQueryHandler(
            ILogger<GetPublicProductsQueryHandler> logger,
            IApplicationDbContext dbContext,
            IQueryHelperService queryHelperService,
            IExchangeRateService exchangeRateService,
            IConfiguration configuration,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _queryHelperService = queryHelperService;
            _exchangeRateService = exchangeRateService;
            _configuration = configuration;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<PagingResponse<ProductPublicResponse>> Handle(GetPublicProductsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                IQueryable<Product> query = _dbContext.Products
                    .AsNoTracking()
                    .Include(p => p.User)
                        .ThenInclude(u => u.Profile)
                    .Include(p => p.ProductMediaFiles)
                        .ThenInclude(pmf => pmf.MediaFile)
                    .Include(p => p.ProductVariant)
                        .ThenInclude(pv => pv.ProductVariantOptions)
                    .Include(p => p.ProductVariantCombinations)
                        .ThenInclude(pvc => pvc.CombinationOptions)
                            .ThenInclude(co => co.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                    .Include(p => p.Country);
                query = query.Where(e => e.IsActive == true);

                // Filter by username(s) if provided (comma-separated for multiple sellers)
                if (!String.IsNullOrWhiteSpace(request.Username))
                {
                    var requestedUsernames = request.Username
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Distinct()
                        .ToList();

                    if (requestedUsernames.Any())
                    {
                        // Load target profiles for all requested usernames in one query
                        var targetProfiles = await _dbContext.Profiles
                            .Include(p => p.ProfileSettings)
                            .Include(p => p.User)
                            .Where(p => requestedUsernames.Contains(p.User.UserName))
                            .ToListAsync(cancellationToken);

                        // Resolve the viewer's profile once (may be null for anonymous callers)
                        var currentUserId = _currentUserService.GetUserId();
                        Core.Domain.Entities.Profile currentProfile = null;
                        if (!string.IsNullOrWhiteSpace(currentUserId))
                        {
                            var currentUser = await _dbContext.Users
                                .Include(u => u.Profile)
                                .SingleOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
                            currentProfile = currentUser?.Profile;
                        }

                        // Privacy check: silently skip private profiles the viewer can't access
                        var allowedUsernames = new List<string>();
                        foreach (var profile in targetProfiles)
                        {
                            var isPrivate = profile.ProfileSettings != null && !profile.ProfileSettings.IsProfilePublic;
                            if (!isPrivate)
                            {
                                allowedUsernames.Add(profile.User.UserName);
                                continue;
                            }

                            var isOwner = currentProfile != null && currentProfile.Uid == profile.Uid;
                            var isFollower = false;
                            if (!isOwner && currentProfile != null)
                            {
                                isFollower = await _dbContext.ProfileFollowers.AnyAsync(
                                    pf => pf.ProfileId == profile.Id && pf.FollowerId == currentProfile.Id,
                                    cancellationToken);
                            }

                            if (isOwner || isFollower)
                            {
                                allowedUsernames.Add(profile.User.UserName);
                            }
                        }

                        // If nothing is accessible this yields an empty paged result (no exception)
                        query = query.Where(p => allowedUsernames.Contains(p.User.UserName));
                    }
                }

                // Filter by product type if provided
                if (request.Type.HasValue)
                {
                    query = query.Where(p => p.Type == request.Type.Value);
                }

                // Filter by CollabId if provided
                if (!string.IsNullOrWhiteSpace(request.CollabId))
                {
                    query = query.Where(p => p.CollabId == request.CollabId);
                }                             

                if (!String.IsNullOrWhiteSpace(request.Search))
                {
                    query = query.Where(p => p.Name.ToLower().Contains(request.Search.Trim().ToLower()) ||
                                             p.ProductDetail.ToLower().Contains(request.Search.Trim().ToLower()) ||
                                             p.Brand.ToLower().Contains(request.Search.Trim().ToLower()) 
                                             );
                }

                if (String.IsNullOrWhiteSpace(request.Order) || String.IsNullOrWhiteSpace(request.OrderBy))
                {
                    query = query.OrderByDescending(u => u.Id);
                }
                else
                {
                    query = _queryHelperService.AppendOrderBy(query, request.OrderBy, request.Order);
                }

                var list = await PagedList<Product>.ToPagedListAsync(query, request.PageNumber, request.PageSize);

                // Get active product IDs that have processing orders
                var pagedProductIds = list.Select(p => p.Id).ToList();
                var processingProductIds = await _dbContext.OrderProductAffiliates
                    .Where(opa => pagedProductIds.Contains(opa.ProductId) && 
                                 (opa.Order.OrderStatus == OrderStatusEnum.Pending || 
                                  opa.Order.OrderStatus == OrderStatusEnum.Processing))
                    .Select(opa => opa.ProductId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Manual mapping to avoid AutoMapper issues with new ProductVariantCombinations
                var mappedItems = list.Select(product => new ProductPublicResponse
                {
                    Uid = product.Uid,
                    IsDeletable = !processingProductIds.Contains(product.Id),
                    Name = product.Name,
                    WhatIsIt = product.WhatIsIt,
                    ProductDetail = product.ProductDetail,
                    Brand = product.Brand,
                    MinPrice = product.MinPrice,
                    MaxPrice = product.MaxPrice,
                    CountryCode = product.Country?.Iso3,
                    CurrencyCode = product.Country?.Iso4,
                    ProductUrl = product.ProductUrl,
                    Type = product.Type,
                    SellType = product.SellType,
                    CollabId = product.CollabId,
                    ProductMediaFiles = product.ProductMediaFiles
                        .Where(pmf => pmf.MediaFile.IsActive)
                        .Select(pmf => new MediaFileDetailsResponse
                        {
                            Uid = pmf.MediaFile.Uid,
                            FileType = pmf.MediaFile.MediaFileType.ToString(),
                            Url = pmf.MediaFile.Url,
                            Priority = pmf.MediaFile.Priority,
                            IsHlsProcessed = pmf.MediaFile.IsHlsProcessed,
                            OriginalUrl = pmf.MediaFile.OriginalUrl,
                            HlsBasePath = pmf.MediaFile.HlsBasePath,
                            VideoDurationSeconds = pmf.MediaFile.VideoDurationSeconds,
                            AvailableQualities = pmf.MediaFile.AvailableQualities,
                            IsMuted = pmf.MediaFile.IsMuted,
                            CropX = pmf.MediaFile.CropX,
                            CropY = pmf.MediaFile.CropY,
                            CropWidth = pmf.MediaFile.CropWidth,
                            CropHeight = pmf.MediaFile.CropHeight
                        }).ToList(),
                    ProductVariants = product.ProductVariant?.Where(pv => 
                        {
                            // Hide variants if it's a "standard" variant with only the product name as option
                            var isStandardVariant = pv.VariantName.Equals("standard", StringComparison.OrdinalIgnoreCase);
                            var options = pv.ProductVariantOptions?.Select(pvo => pvo.Value).ToList() ?? new List<string>();
                            var hasOnlyProductNameOption = options.Count == 1 && 
                                options.Any(opt => opt.Equals(product.Name, StringComparison.OrdinalIgnoreCase));
                            
                            // Return false to filter out (hide) standard variants with only product name
                            return !(isStandardVariant && hasOnlyProductNameOption);
                        })
                        .Select(variant => new ProductVariantResponse
                        {
                            VariantName = variant.VariantName,
                            VariantOptions = variant.ProductVariantOptions?.Select(opt => opt.Value).ToList() ?? new List<string>()
                        }).ToList() ?? new List<ProductVariantResponse>(),
                    ProductVariantCombinations = (product.ProductVariantCombinations?
                        .Where(pvc => pvc.IsActive)
                        .GroupBy(pvc => pvc.CombinationOptions.FirstOrDefault()?.ProductVariantOption?.Value ?? "Default")
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(pvc => new ProductVariantCombinationResponse
                            {
                                Uid = pvc.Uid,
                                SKU = pvc.SKU,
                                Price = pvc.Price,
                                Quantity = pvc.Quantity,
                                ImageUrl = pvc.ImageUrl,
                                IsAvailable = pvc.IsAvailable,
                                DisplayName = string.Join(", ", pvc.CombinationOptions.Select(co => co.ProductVariantOption.Value)),
                                VariantValues = pvc.CombinationOptions
                                    .OrderBy(co => co.ProductVariantOption.ProductVariant.Id)
                                    .Select(co => co.ProductVariantOption.Value)
                                    .ToArray()
                            }).ToList()
                        )) ?? new Dictionary<string, List<ProductVariantCombinationResponse>>(),
                    Profile = product.User?.Profile != null ? new ProfileBaseResponse
                    {
                        Uid = product.User.Profile.Uid,
                        UserId = product.User.Id,
                        ImageUrl = product.User.Profile.ImageUrl,
                        FullName = product.User.FirstName,
                        LastName = product.User.LastName,
                        Username = product.User.UserName,
                        DisplayName = product.User.DisplayName,
                        UserType = product.User.Profile.UserType,
                        FollowedByMe = false
                    } : null
                }).ToList();

                var mappedList = new PagingResponse<ProductPublicResponse>
                {
                    Items = mappedItems,
                    CurrentPage = list.CurrentPage,
                    PageSize = list.PageSize,
                    TotalCount = list.TotalCount,
                    TotalPages = list.TotalPages
                };

                mappedList.ItemIds = mappedList.Items.Select(item => item.Uid).ToList();

                var viewerUserId = _currentUserService.GetUserId();
                if (!string.IsNullOrWhiteSpace(viewerUserId))
                {
                    mappedList.CurrentUserBagItemsCount = await _dbContext.UserBagProducts
                        .CountAsync(p => p.UserId == viewerUserId, cancellationToken);
                    mappedList.CurrentUserWishlistCount = await _dbContext.UserWishlistProducts
                        .CountAsync(p => p.UserId == viewerUserId, cancellationToken);
                    mappedList.CurrentUserBagItemsTotalQuantity = await _dbContext.UserBagProducts
                        .Where(p => p.UserId == viewerUserId)
                        .SumAsync(p => (int?)p.Quantity ?? 0, cancellationToken);
                }

                return mappedList;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}