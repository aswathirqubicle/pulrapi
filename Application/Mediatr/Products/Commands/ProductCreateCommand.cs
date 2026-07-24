using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Security.Validation.Attributes;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using shortid;
using shortid.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Products.Commands
{
    public class ProductCreateCommand : IRequest<ProductDetailsResponse>
    {
        public string Name { get; set; }

        public string WhatIsIt { get; set; }

        public string ProductDetail { get; set; }
        
        public string Brand { get; set; }

        public double? MinPrice { get; set; }

        public double? MaxPrice { get; set; }

        [SafeUid(allowNullValue: true, ErrorMessage = "Country UID contains invalid characters or format.")]
        public string CountryUid { get; set; }
        
        public string ProductUrl { get; set; }

        public ProductTypeEnum Type { get; set; } = ProductTypeEnum.Product;
        public ProductSellTypeEnum SellType { get; set; } = ProductSellTypeEnum.SellOnPulr;

        [Required(ErrorMessage = "Media files are required.")]
        [MaxLength(5, ErrorMessage = "Maximum 5 images allowed")]
        public List<string> MediaFileUids { get; set; } = new List<string>();

        [MaxLength(3, ErrorMessage = "Maximum 3 variants allowed")]
        public List<ProductVariantCreateRequest> ProductVariants { get; set; } = new List<ProductVariantCreateRequest>();

        // Option 1: Auto-generate all combinations from variants (Cartesian product)
        public bool AutoGenerateCombinations { get; set; } = false;

        // Option 2: Manually specify each combination with SKU, price, and inventory
        public List<ProductVariantCombinationRequest> ProductVariantCombinations { get; set; } = new List<ProductVariantCombinationRequest>();

        // Default values for auto-generated combinations
        public decimal? DefaultCombinationPrice { get; set; }
        public int DefaultQuantity { get; set; } = 5;

        [SafeUid(allowNullValue: true, ErrorMessage = "CollabId contains invalid characters or format.")]
        public string? CollabId { get; set; }
    }

    public class ProductCreateCommandHandler : IRequestHandler<ProductCreateCommand, ProductDetailsResponse>
    {
        private readonly ILogger<ProductCreateCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;
        private readonly IBrandService _brandService;
        private readonly ProductVariantService _variantService;

        public ProductCreateCommandHandler(
            ILogger<ProductCreateCommandHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext,
            IBrandService brandService)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
            _brandService = brandService;
            _variantService = new ProductVariantService();
        }

        public async Task<ProductDetailsResponse> Handle(ProductCreateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync(includeStores: true) ?? throw new UnauthorizedAccessException("User not found or not logged in");

                // Validate brand is required when type is "own"
                if (request.Type == ProductTypeEnum.Own && string.IsNullOrWhiteSpace(request.Brand))
                {
                    throw new BadRequestException("Brand is required when creating an 'own' tag");
                }

                // Price validation removed per requirements

                //validate the MediaFiles
                if (request.MediaFileUids.Count != 0)
                {
                    var activeMediaFiles = await _dbContext.MediaFiles
                        .Where(mf => mf.IsActive && request.MediaFileUids.Contains(mf.Uid))
                        .ToListAsync(cancellationToken);

                    if (activeMediaFiles.Count != request.MediaFileUids.Count)
                    {
                        throw new BadRequestException("Some media files not found or inactive");
                    }
                }

                // Handle brand creation/retrieval
                var brandName = await _brandService.GetOrCreateBrandAsync(request.Brand, cancellationToken);

                var product = new Product
                {
                    Name = request.Name.Trim(),
                    WhatIsIt = request.WhatIsIt,
                    ProductDetail = request.ProductDetail?.Trim(),
                    Brand = brandName,
                    MinPrice = request.MinPrice,
                    MaxPrice = request.MaxPrice,
                    CountryUid = request.CountryUid,
                    ProductUrl = request.ProductUrl?.Trim(),
                    Type = request.Type,
                    SellType = request.SellType,
                    UserId = user.Id,
                    CollabId = request.CollabId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _dbContext.Products.Add(product);
                await _dbContext.SaveChangesAsync(cancellationToken);

                //add media files
                if (request.MediaFileUids.Count > 0)
                {
                    var productMediaFiles = request.MediaFileUids.Select(mediaFileUid => new ProductMediaFile
                    {
                        Product = product,
                        MediaFile = _dbContext.MediaFiles.First(mf => mf.Uid == mediaFileUid),
                    }).ToList();

                    _dbContext.ProductMediaFiles.AddRange(productMediaFiles);
                }

                var productVariants = new List<ProductVariant>();
                var productVariantCombinations = new List<ProductVariantCombination>();

                if(request.ProductVariants != null && request.ProductVariants.Count != 0)
                { 
                    //add variants
                    productVariants = request.ProductVariants.Select(variant => new ProductVariant
                    {
                        VariantName = variant.VariantName?.Trim(),
                        Product = product,
                        ProductVariantOptions = (variant.VariantOptions ?? new List<string>()).Select(option => new ProductVariantOption
                        {
                            Value = option
                        }).ToList()
                    }).ToList();

                    _dbContext.ProductVariants.AddRange(productVariants);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Generate variant combinations
                    if (request.AutoGenerateCombinations)
                    {
                        // Auto-generate all combinations using Cartesian product
                        var combinations = _variantService.GenerateVariantCombinations(productVariants);
                        productVariantCombinations = _variantService.CreateCombinationEntities(
                            product.Id,
                            product.Name,
                            combinations,
                            request.DefaultCombinationPrice ?? (request.MinPrice.HasValue ? (decimal)request.MinPrice.Value : (decimal?)null),
                            request.DefaultQuantity
                        );

                        _dbContext.ProductVariantCombinations.AddRange(productVariantCombinations);
                    }
                    else if (request.ProductVariantCombinations != null && request.ProductVariantCombinations.Count > 0)
                    {
                        // Manually specified combinations
                        foreach (var combRequest in request.ProductVariantCombinations)
                        {
                            var combination = new ProductVariantCombination
                            {
                                ProductId = product.Id,
                                SKU = combRequest.SKU,
                                Price = combRequest.Price,
                                Quantity = combRequest.Quantity,
                                ImageUrl = combRequest.ImageUrl,
                                IsAvailable = combRequest.IsAvailable,
                                CombinationOptions = new List<ProductVariantCombinationOption>()
                            };

                            // Map variant values to ProductVariantOptions
                            for (int i = 0; i < combRequest.VariantValues.Length && i < productVariants.Count; i++)
                            {
                                var variantValue = combRequest.VariantValues[i];
                                var matchingOption = productVariants[i].ProductVariantOptions
                                    .FirstOrDefault(opt => opt.Value.Equals(variantValue, StringComparison.OrdinalIgnoreCase));

                                if (matchingOption != null)
                                {
                                    combination.CombinationOptions.Add(new ProductVariantCombinationOption
                                    {
                                        ProductVariantOptionId = matchingOption.Id
                                    });
                                }
                            }

                            productVariantCombinations.Add(combination);
                        }

                        _dbContext.ProductVariantCombinations.AddRange(productVariantCombinations);
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Get media files for response
                var mediaFiles = new List<MediaFileDetailsResponse>();
                if (request.MediaFileUids.Count > 0)
                {
                    mediaFiles = await _dbContext.MediaFiles
                        .Where(mf => request.MediaFileUids.Contains(mf.Uid))
                        .Select(mf => new MediaFileDetailsResponse
                        {
                            Uid = mf.Uid,
                            FileType = mf.MediaFileType.ToString(),
                            Url = mf.Url
                        })
                        .ToListAsync(cancellationToken);
                }

                // Get country data for response
                var country = await _dbContext.Countries
                    .Where(c => c.Uid == request.CountryUid)
                    .Select(c => new { c.Iso2, c.Iso4 })
                    .FirstOrDefaultAsync(cancellationToken);

                // Map to response
var productResponse = new ProductDetailsResponse
                {
                    Uid = product.Uid,
                    Name = product.Name,
                    WhatIsIt = product.WhatIsIt,
                    ProductDetail = product.ProductDetail,
                    Brand = product.Brand,
                    MinPrice = product.MinPrice,
                    MaxPrice = product.MaxPrice,
                    CountryCode = country?.Iso2,
                    CurrencyCode = country?.Iso4,
                    ProductUrl = product.ProductUrl,
                    Type = product.Type,
                    SellType = product.SellType,
                    CollabId = product.CollabId,
                    CreatedAt = product.CreatedAt,
                    ProductMediaFiles = mediaFiles,
                    ProductVariants = productVariants.Select(pv => new ProductVariantResponse
                    {
                        VariantName = pv.VariantName,
                        VariantOptions = pv.ProductVariantOptions?.Select(pvo => pvo.Value).ToList() ?? new List<string>()
                    }).ToList(),
                    ProductVariantCombinations = productVariantCombinations
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
                                DisplayName = _variantService.GenerateDisplayName(
                                    pvc.CombinationOptions.Select(co => co.ProductVariantOption).ToList()
                                ),
                                VariantValues = pvc.CombinationOptions
                                    .OrderBy(co => co.ProductVariantOption.ProductVariant.Id)
                                    .Select(co => co.ProductVariantOption.Value)
                                    .ToArray()
                            }).ToList()
                        ),
                    Profile = new ProfileBaseResponse
                    {
                        Uid = user.Profile.Uid,
                        UserId = user.Id,
                        ImageUrl = user.Profile.ImageUrl,
                        //IsStore = false,
                        FullName = user.FirstName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Username = user.UserName,
                        DisplayName = user.DisplayName,
                        UserType = user.Profile.UserType,
                        FollowedByMe = false
                    }
                };
                return productResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}