using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Orders.Commands.Create;
using Core.Application.Models.Orders;
using Core.Domain.Entities;
using Core.Domain.Enums;
using ShippingDetailsEntity = Core.Domain.Entities.ShippingDetails;

namespace Core.Application.Mediatr.Orders.Commands.Create;


public class CreateOrderCommand : IRequest<string>, ICloneable
{
    public PaymentMethodEnum PaymentMethod { get; set; }
    public string CurrencyUid { get; set; }
    public CardDetailsDto CardDetails { get; set; }
    public List<OrderProductDto> Products { get; set; } = new List<OrderProductDto>();
    
    /// <summary>
    /// Optional billing address UID. If null, uses the default shipping address for billing.
    /// </summary>
    public string BillingAddressUid { get; set; }

    public object Clone()
    {
        var codSafeClone = (CreateOrderCommand)MemberwiseClone();
        // we skip card details when logging
        codSafeClone.CardDetails = null;
        return codSafeClone;
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, string>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public CreateOrderCommandHandler(IApplicationDbContext dbContext, ILogger<CreateOrderCommandHandler> logger, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<string> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync(true);

            var paymentMethod = await _dbContext.PaymentMethods.SingleOrDefaultAsync(e => e.Key == request.PaymentMethod.ToString());

            var profile = await _dbContext.Profiles.Where(p => p.User.Id == cUser.Id).Include(p => p.User).ThenInclude(u => u.ShippingDetails).SingleOrDefaultAsync();
            if (profile == null)
            {
                throw new NotFoundException("Profile not found.");
            }

            if (profile.User.ShippingDetails == null || profile.User.ShippingDetails.Count == 0)
            {
                throw new NotFoundException($"User doesn't have shipping details.");
            }

            var currency = await _dbContext.Currencies.SingleOrDefaultAsync(e => e.Uid == request.CurrencyUid, cancellationToken);

            var orderProductAffiliates = new List<OrderProductAffiliate>();

            foreach (var productDto in request.Products)
            {
                // Load product with all related data to capture complete snapshot
                var existingProduct = await _dbContext.Products
                    .Include(p => p.ProductMediaFiles)
                        .ThenInclude(pmf => pmf.MediaFile)
                    .Include(p => p.ProductVariantCombinations)
                        .ThenInclude(pvc => pvc.CombinationOptions)
                            .ThenInclude(co => co.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                    .Include(p => p.Country)
                    .Include(p => p.User)
                        .ThenInclude(u => u.Profile)
                    .SingleOrDefaultAsync(p => p.Uid == productDto.Uid, cancellationToken);
                if (existingProduct == null)
                {
                    throw new NotFoundException($"Product with uid {productDto.Uid} doesn't exist.");
                }

                // Capture product snapshot data
                var productName = existingProduct.Name ?? string.Empty;
                var productDescription = string.IsNullOrWhiteSpace(existingProduct.ProductDetail)
                    ? existingProduct.WhatIsIt ?? string.Empty
                    : $"{existingProduct.WhatIsIt ?? string.Empty} {existingProduct.ProductDetail}".Trim();

                // Determine price: use price from DTO (which may come from variant combination), 
                // or fallback to product MinPrice, or use variant combination price if available
                decimal? productPrice = null;
                if (productDto.Price > 0)
                {
                    productPrice = productDto.Price;
                }
                else if (existingProduct.MinPrice.HasValue)
                {
                    productPrice = (decimal)existingProduct.MinPrice.Value;
                }

                // Get primary image URL (Priority == 0, or first active image)
                var primaryImageUrl = existingProduct.ProductMediaFiles?
                    .Where(pmf => pmf.MediaFile != null && pmf.MediaFile.IsActive)
                    .OrderBy(pmf => pmf.MediaFile.Priority)
                    .FirstOrDefault()?.MediaFile.Url;

                // Get seller settings for shipping cost and delivery time
                decimal? shippingCost = null;
                string deliveryTime = "7 days"; // Default fallback
                if (!string.IsNullOrEmpty(existingProduct.UserId))
                {
                    var sellerSettings = await _dbContext.SellerSettings
                        .FirstOrDefaultAsync(ss => ss.UserId == existingProduct.UserId, cancellationToken);
                    if (sellerSettings != null)
                    {
                        shippingCost = sellerSettings.ShippingCosts;
                        deliveryTime = sellerSettings.DeliveryTime ?? "7 days";
                    }
                }

                // Get specific variant combination if UID provided, or fallback to first active combination
                ProductVariantCombination? selectedVariantCombination = null;
                if (!string.IsNullOrWhiteSpace(productDto.VariantCombinationUid))
                {
                    selectedVariantCombination = existingProduct.ProductVariantCombinations?
                        .FirstOrDefault(vc => vc.Uid == productDto.VariantCombinationUid);
                }

                var targetCombination = selectedVariantCombination ?? existingProduct.ProductVariantCombinations?.FirstOrDefault(vc => vc.IsActive);
                var variantTypes = new List<string>();
                
                if (targetCombination?.CombinationOptions != null)
                {
                    variantTypes = targetCombination.CombinationOptions
                        .OrderBy(co => co.ProductVariantOption?.ProductVariant?.Id ?? 0)
                        .Select(co => co.ProductVariantOption?.Value)
                        .Where(v => !string.IsNullOrEmpty(v))
                        .ToList();
                }

                // If specialized variant price exists, use it as fallback if DTO price is 0
                if (productDto.Price <= 0 && targetCombination?.Price.HasValue == true)
                {
                    productPrice = targetCombination.Price.Value;
                }

                // If specialized variant image exists, use it as primary image
                if (!string.IsNullOrWhiteSpace(targetCombination?.ImageUrl))
                {
                    primaryImageUrl = targetCombination.ImageUrl;
                }

                var deliveryDays = ParseDeliveryDays(deliveryTime);
                var countdownExpiryDate = DateTime.UtcNow.AddDays(deliveryDays);

                var opAff = new OrderProductAffiliate()
                {
                    Affiliate = await _dbContext.Affiliates.SingleOrDefaultAsync(a => a.AffiliateId == productDto.AffiliateId, cancellationToken),
                    Product = existingProduct,
                    ProductQuantity = productDto.BagQuantity,
                    ProductVariantCombinationId = selectedVariantCombination?.Id,
                    ProductVariantCombinationUidSnapshot = selectedVariantCombination?.Uid,
                    // Capture complete snapshot data
                    ProductNameSnapshot = productName,
                    ProductDescriptionSnapshot = productDescription,
                    ProductPriceSnapshot = productPrice,
                    ProductMinPriceSnapshot = existingProduct.MinPrice,
                    ProductMaxPriceSnapshot = existingProduct.MaxPrice,
                    ProductBrandSnapshot = existingProduct.Brand,
                    PrimaryImageUrlSnapshot = primaryImageUrl,
                    CountryCodeSnapshot = existingProduct.Country?.Iso2,
                    CurrencyCodeSnapshot = existingProduct.Country?.Iso4,
                    ProductTypeSnapshot = (int)existingProduct.Type,
                    ProfileUidSnapshot = existingProduct.User?.Profile?.Uid,
                    ProfileUsernameSnapshot = existingProduct.User?.UserName,
                    ShippingCostSnapshot = shippingCost,
                    DeliveryTimeSnapshot = deliveryTime,
                    CountdownExpiryDate = countdownExpiryDate,
                    VariantTypesSnapshot = variantTypes.Any() ? JsonConvert.SerializeObject(variantTypes) : null
                };


                if (opAff != null)
                {
                    orderProductAffiliates.Add(opAff);
                }
            }

            // Get default shipping address (IsBillingAddress = false)
            var shippingAddress = profile.User.ShippingDetails
                .SingleOrDefault(e => e.DefaultShippingAddress == true && e.IsBillingAddress == false);
            
            if (shippingAddress == null)
            {
                throw new NotFoundException("No default shipping address found. Please set a default shipping address.");
            }
            
            // Get billing address if specified, otherwise use default billing or fall back to shipping
            ShippingDetailsEntity billingAddress = null;
            if (!string.IsNullOrWhiteSpace(request.BillingAddressUid))
            {
                billingAddress = await _dbContext.ShippingDetails
                    .SingleOrDefaultAsync(sd => sd.Uid == request.BillingAddressUid && sd.UserId == cUser.Id && sd.IsActive, cancellationToken);
                
                if (billingAddress == null)
                {
                    throw new NotFoundException($"Billing address with UID {request.BillingAddressUid} not found.");
                }
            }
            else
            {
                // Try to get default billing address
                billingAddress = profile.User.ShippingDetails
                    .SingleOrDefault(e => e.DefaultShippingAddress == true && e.IsBillingAddress == true);
                // If no default billing address, billingAddress remains null and shipping will be used
            }

            // create order at pulr
            var order = new Order()
            {
                OrderProductAffiliates = orderProductAffiliates,
                Currency = currency,
                PaymentMethod = paymentMethod,
                Profile = profile,
                ShippingDetails = shippingAddress,
                BillingDetails = billingAddress, // Will be null if same as shipping
                RawRequest = JsonConvert.SerializeObject(request.Clone()),
                OrderStatus = OrderStatusEnum.Pending
            };

            _dbContext.Orders.Add(order);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Non-guessable, non-sequential public order id to prevent IDOR enumeration.
            var formattedOrderId = OrderUidGenerator.Generate();
            order.Uid = formattedOrderId;

            // Update individual product (sub-order) IDs as "{orderUid}-01", "{orderUid}-02", etc.
            int productIndex = 1;
            foreach (var opa in order.OrderProductAffiliates)
            {
                opa.Uid = $"{formattedOrderId}-{productIndex:D2}";
                productIndex++;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return order.Uid;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }

    private static double ParseDeliveryDays(string deliveryTime)
    {
        if (string.IsNullOrWhiteSpace(deliveryTime)) return 7;
        var dt = deliveryTime.ToLower().Trim();
        var rangeMatch = Regex.Match(dt, @"(\d+)\s*-\s*(\d+)\s*(day|week)s?");
        if (rangeMatch.Success)
            return rangeMatch.Groups[3].Value == "week"
                ? int.Parse(rangeMatch.Groups[2].Value) * 7
                : int.Parse(rangeMatch.Groups[2].Value);
        var singleMatch = Regex.Match(dt, @"(\d+)\s*(day|week)s?");
        if (singleMatch.Success)
            return singleMatch.Groups[2].Value == "week"
                ? int.Parse(singleMatch.Groups[1].Value) * 7
                : int.Parse(singleMatch.Groups[1].Value);
        var minuteMatch = Regex.Match(dt, @"(\d+)\s*(minute|min)s?");
        if (minuteMatch.Success) return double.Parse(minuteMatch.Groups[1].Value) / 1440.0;
        var hourMatch = Regex.Match(dt, @"(\d+)\s*hours?");
        if (hourMatch.Success) return double.Parse(hourMatch.Groups[1].Value) / 24.0;
        var numMatch = Regex.Match(dt, @"(\d+)");
        return numMatch.Success ? int.Parse(numMatch.Groups[1].Value) : 7;
    }
}
