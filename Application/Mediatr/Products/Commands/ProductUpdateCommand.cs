using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Products;
using Core.Application.Models.Profiles;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Core.Application.Mediatr.Products.Commands
{
    public class ProductUpdateCommand : IRequest<ProductDetailsResponse>
    {
        //[Required(ErrorMessage = "Product UID is required.")]
        [SafeUid(allowNullValue: false, ErrorMessage = "Product UID contains invalid characters or format.")]
        public string Uid { get; set; }

        public string Name { get; set; }
        
        public string WhatIsIt { get; set; }
        public string ProductDetail { get; set; }
        
        public string Brand { get; set; }

        public double? MinPrice { get; set; }

        public double? MaxPrice { get; set; }
        
        [SafeUid(allowNullValue: true, ErrorMessage = "Country UID contains invalid characters or format.")]
        public string CountryUid { get; set; }
        public string ProductUrl { get; set; }
        public ProductTypeEnum Type { get; set; }
        public ProductSellTypeEnum SellType { get; set; }

        [Required(ErrorMessage = "Media files are required.")]
        [MaxLength(5, ErrorMessage = "Maximum 5 images allowed")]
        public List<string> MediaFileUids { get; set; } = [];

        [MaxLength(3, ErrorMessage = "Maximum 3 variants allowed")]
        public List<ProductVariantCreateRequest> ProductVariants { get; set; } = [];

        public List<ProductVariantCombinationUpdateRequest> ProductVariantCombinations { get; set; } = [];
    }

    public class ProductUpdateCommandHandler : IRequestHandler<ProductUpdateCommand, ProductDetailsResponse>
    {
        private readonly ILogger<ProductUpdateCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IBrandService _brandService;

        public ProductUpdateCommandHandler(
            ILogger<ProductUpdateCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IBrandService brandService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _brandService = brandService;
        }

        public async Task<ProductDetailsResponse> Handle(ProductUpdateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _currentUserService.GetUserAsync(includeStores: true) ?? throw new UnauthorizedAccessException("User not found or not logged in");

                var product = await _dbContext.Products
                    .Include(p => p.ProductMediaFiles)
                        .ThenInclude(pm => pm.MediaFile)
                    .SingleOrDefaultAsync(p => p.Uid == request.Uid && p.UserId == user.Id && p.IsActive, cancellationToken);

                if (product == null)
                {
                    throw new NotFoundException("Product not found");
                }

                //validate the price range
                // if (request.MinPrice.HasValue && request.MaxPrice.HasValue &&
                //    (request.MinPrice <= 0 || request.MaxPrice <= 0 || request.MinPrice > request.MaxPrice))
                // {
                //     throw new BadRequestException("Invalid price range");
                // }

                if (!String.IsNullOrWhiteSpace(request.Name))
                    product.Name = request.Name.Trim();

                if (!String.IsNullOrWhiteSpace(request.ProductDetail))
                {
                    product.ProductDetail = request.ProductDetail;
                }

                if (!string.IsNullOrWhiteSpace(request.WhatIsIt))
                    product.WhatIsIt = request.WhatIsIt;


                if (request.MinPrice.HasValue)
                    product.MinPrice = request.MinPrice.Value;

                if (request.MaxPrice.HasValue)
                    product.MaxPrice = request.MaxPrice.Value;

                if (!string.IsNullOrWhiteSpace(request.CountryUid))
                    product.CountryUid = request.CountryUid;

                if (request.ProductUrl != null)
                    product.ProductUrl = request.ProductUrl;

                // Update Type field
                product.Type = request.Type;
                product.SellType = request.SellType;

                // Validate brand is required when type is "own"
                if (request.Type == ProductTypeEnum.Own && string.IsNullOrWhiteSpace(request.Brand))
                {
                    throw new BadRequestException("Brand is required when updating to 'own' tag type");
                }

                // Handle brand creation/retrieval
                if (!string.IsNullOrWhiteSpace(request.Brand))
                {
                    var brandName = await _brandService.GetOrCreateBrandAsync(request.Brand, cancellationToken);
                    product.Brand = brandName;
                }

                if (request.MediaFileUids != null)
                {

                    // Remove existing media files
                    if (product.ProductMediaFiles != null)
                    {
                        _dbContext.ProductMediaFiles.RemoveRange(product.ProductMediaFiles);
                    }

                    //Add new update media files
                    if (request.MediaFileUids != null && request.MediaFileUids.Count > 0)
                    {
                        var mediaFiles = await _dbContext.MediaFiles
                            .Where(mf => mf.IsActive && request.MediaFileUids.Contains(mf.Uid))
                            .ToListAsync(cancellationToken);
                        if (mediaFiles.Count != request.MediaFileUids.Count)
                        {
                            throw new BadRequestException("Some media files not found or inactive");
                        }

                        // Add new media files
                        var productMediaFiles = request.MediaFileUids.Select(mediaFileUid => new ProductMediaFile
                        {
                            Product = product,
                            MediaFile = mediaFiles.First(mf => mf.Uid == mediaFileUid)
                        }).ToList();

                        _dbContext.ProductMediaFiles.AddRange(productMediaFiles);
                    }
                }

                //update variants only if explicitly provided
                if (request.ProductVariants != null && request.ProductVariants.Any())
                {
                    // Load existing variants from database
                    var existingVariants = await _dbContext.ProductVariants
                        .Include(pv => pv.ProductVariantOptions)
                        .Where(pv => pv.ProductId == product.Id)
                        .ToListAsync(cancellationToken);

                    // Check if variants have actually changed
                    var variantsChanged = !existingVariants.Any() || 
                        existingVariants.Count != request.ProductVariants.Count ||
                        existingVariants.Any(ev => !request.ProductVariants.Any(rv => 
                            rv.VariantName.Equals(ev.VariantName, StringComparison.OrdinalIgnoreCase)));

                    // Remove existing variants and their options only if they changed
                    if (variantsChanged && existingVariants.Any())
                    {
                        // First, get all variant option IDs that will be deleted
                        var variantOptionIds = existingVariants
                            .SelectMany(v => v.ProductVariantOptions.Select(pvo => pvo.Id))
                            .ToList();

                        // Delete ProductVariantCombinationOptions that reference these variant options
                        if (variantOptionIds.Any())
                        {
                            var combinationOptionsToDelete = await _dbContext.ProductVariantCombinationOptions
                                .Where(pvco => variantOptionIds.Contains(pvco.ProductVariantOptionId))
                                .ToListAsync(cancellationToken);

                            if (combinationOptionsToDelete.Any())
                            {
                                _dbContext.ProductVariantCombinationOptions.RemoveRange(combinationOptionsToDelete);
                            }
                        }

                        // Then delete ProductVariantOptions
                        foreach (var variant in existingVariants)
                        {
                            if (variant.ProductVariantOptions != null)
                            {
                                _dbContext.ProductVariantOptions.RemoveRange(variant.ProductVariantOptions);
                            }
                        }

                        // Finally delete ProductVariants
                        _dbContext.ProductVariants.RemoveRange(existingVariants);
                    }

                    // Add new variants only if they changed
                    if (variantsChanged)
                    {
                        var productVariants = request.ProductVariants.Select(variantRequest => new ProductVariant
                        {
                            Product = product,
                            VariantName = variantRequest.VariantName?.Trim(),
                            ProductVariantOptions = variantRequest.VariantOptions?.Select(option => new ProductVariantOption
                            {
                                Value = option
                            }).ToList() ?? []
                        }).ToList();

                        _dbContext.ProductVariants.AddRange(productVariants);
                        
                        // Save variants to database so they get IDs before creating combinations
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }

                // Handle ProductVariantCombinations updates and creation
                if (request.ProductVariantCombinations != null && request.ProductVariantCombinations.Any())
                {
                    // Load current variants for the product to map variant values
                    var currentVariants = await _dbContext.ProductVariants
                        .Include(pv => pv.ProductVariantOptions)
                        .Where(pv => pv.ProductId == product.Id)
                        .ToListAsync(cancellationToken);

                    // Check if any new combinations are being added (no Uid provided)
                    var hasNewCombinations = request.ProductVariantCombinations.Any(c => string.IsNullOrEmpty(c.Uid));
                    
                    // If new combinations are being added, delete all existing combinations for this product
                    if (hasNewCombinations)
                    {
                        var existingCombinations = await _dbContext.ProductVariantCombinations
                            .Where(pvc => pvc.ProductId == product.Id)
                            .ToListAsync(cancellationToken);
                        
                        if (existingCombinations.Any())
                        {
                            _dbContext.ProductVariantCombinations.RemoveRange(existingCombinations);
                            await _dbContext.SaveChangesAsync(cancellationToken);
                        }
                    }

                    foreach (var combRequest in request.ProductVariantCombinations)
                    {
                        if (!string.IsNullOrEmpty(combRequest.Uid))
                        {
                            // Update existing combination
                            var existingCombination = await _dbContext.ProductVariantCombinations
                                .FirstOrDefaultAsync(pvc => pvc.Uid == combRequest.Uid && pvc.ProductId == product.Id, cancellationToken);

                            if (existingCombination != null)
                            {
                                // Update fields if provided
                                if (!string.IsNullOrEmpty(combRequest.SKU))
                                    existingCombination.SKU = combRequest.SKU;
                                
                                if (combRequest.Price.HasValue)
                                    existingCombination.Price = combRequest.Price.Value;
                                
                                if (combRequest.Quantity.HasValue)
                                    existingCombination.Quantity = combRequest.Quantity.Value;
                                
                                if (combRequest.ImageUrl != null)
                                    existingCombination.ImageUrl = combRequest.ImageUrl;
                                
                                if (combRequest.IsAvailable.HasValue)
                                    existingCombination.IsAvailable = combRequest.IsAvailable.Value;

                                existingCombination.UpdatedAt = DateTime.UtcNow;
                                _dbContext.ProductVariantCombinations.Update(existingCombination);
                            }
                        }
                        else
                        {
                            // Create new combination
                            var newCombination = new ProductVariantCombination
                            {
                                ProductId = product.Id,
                                SKU = combRequest.SKU,
                                Price = combRequest.Price,
                                Quantity = combRequest.Quantity ?? 0,
                                ImageUrl = combRequest.ImageUrl,
                                IsAvailable = combRequest.IsAvailable ?? true,
                                CombinationOptions = new List<ProductVariantCombinationOption>()
                            };

                            // Map variant values to ProductVariantOptions
                            if (combRequest.VariantValues != null && combRequest.VariantValues.Length > 0)
                            {
                                // Get all variant options ordered by variant ID to match the order of variantValues
                                var allVariantOptions = currentVariants
                                    .OrderBy(v => v.Id)
                                    .SelectMany(v => v.ProductVariantOptions.Select(opt => new { Variant = v, Option = opt }))
                                    .ToList();

                                // Match each variant value with its corresponding option
                                foreach (var variantValue in combRequest.VariantValues)
                                {
                                    var matchingOption = allVariantOptions
                                        .FirstOrDefault(vo => vo.Option.Value.Equals(variantValue, StringComparison.OrdinalIgnoreCase));

                                    if (matchingOption != null)
                                    {
                                        newCombination.CombinationOptions.Add(new ProductVariantCombinationOption
                                        {
                                            ProductVariantOptionId = matchingOption.Option.Id
                                        });
                                    }
                                }
                            }

                            _dbContext.ProductVariantCombinations.Add(newCombination);
                        }
                    }
                }

                product.UpdatedAt = DateTime.UtcNow;
                _dbContext.Products.Update(product);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Get country data for response
                var country = await _dbContext.Countries
                    .Where(c => c.Uid == product.CountryUid)
                    .Select(c => new { c.Iso2, c.Iso4 })
                    .FirstOrDefaultAsync(cancellationToken);

                // Get ProductVariants and ProductVariantCombinations separately to avoid tracking issues
                var productVariantsForResponse = await _dbContext.ProductVariants
                    .AsNoTracking()
                    .Include(pv => pv.ProductVariantOptions)
                    .Where(pv => pv.ProductId == product.Id)
                    .ToListAsync(cancellationToken);

                var productVariantCombinations = await _dbContext.ProductVariantCombinations
                    .AsNoTracking()
                    .Include(pvc => pvc.CombinationOptions)
                        .ThenInclude(co => co.ProductVariantOption)
                            .ThenInclude(pvo => pvo.ProductVariant)
                    .Where(pvc => pvc.ProductId == product.Id)
                    .ToListAsync(cancellationToken);

                // Check if product has active orders
                bool isDeletable = !await _dbContext.OrderProductAffiliates
                    .AnyAsync(opa => opa.ProductId == product.Id && 
                                   (opa.Order.OrderStatus == OrderStatusEnum.Pending || 
                                    opa.Order.OrderStatus == OrderStatusEnum.Processing), 
                        cancellationToken);

                // Map to ProductDetailsResponse
                var productDetails = new ProductDetailsResponse
                {
                    Uid = product.Uid,
                    IsDeletable = isDeletable,
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
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    ProductMediaFiles = product.ProductMediaFiles?.Select(pmf => new MediaFileDetailsResponse
                    {
                        Uid = pmf.MediaFile.Uid,
                        FileType = pmf.MediaFile.MediaFileType.ToString(),
                        Url = pmf.MediaFile.Url
                    }).ToList() ?? new List<MediaFileDetailsResponse>(),
                    ProductVariants = productVariantsForResponse.Where(pv => 
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
                                DisplayName = string.Join(", ", pvc.CombinationOptions.Select(co => co.ProductVariantOption.Value)),
                                VariantValues = pvc.CombinationOptions
                                    .OrderBy(co => co.ProductVariantOption.ProductVariant.Id)
                                    .Select(co => co.ProductVariantOption.Value)
                                    .ToArray()
                            }).ToList()
                        ),
                    Profile = product.User?.Profile != null ? new ProfileBaseResponse
                    {
                        Uid = product.User.Profile.Uid,
                        UserId = product.User.Id,
                        ImageUrl = product.User.Profile.ImageUrl,
                        //IsStore = false,
                        FullName = product.User.FirstName,
                        LastName = product.User.LastName,
                        Username = product.User.UserName,
                        DisplayName = product.User.DisplayName,
                        UserType = product.User.Profile.UserType,
                        FollowedByMe = false
                    } : null
                };

                return productDetails;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}