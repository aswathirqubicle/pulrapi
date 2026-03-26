using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Orders;
using Core.Application.Models.ShippingDetails;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;
using Core.Application.Exceptions;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Core.Application.Models.Currencies;

namespace Core.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<OrderService> _logger;
    private readonly IEmailService _emailService;

    public OrderService(IApplicationDbContext dbContext, ILogger<OrderService> logger, IEmailService emailService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<PagingResponse<OrderResponse>> GetUserOrdersAsync(
        string userId,
        int pageNumber,
        int pageSize,
        bool checkProcessingOnly,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile == null) throw new NotFoundException("Profile not found.");

        var ordersQuery = _dbContext.Orders
            .Include(o => o.Profile).ThenInclude(p => p.User)
            .Include(o => o.ShippingDetails).ThenInclude(sd => sd.CountryNavigation)
            .Include(o => o.BillingDetails).ThenInclude(bd => bd.CountryNavigation)
            .Where(o => o.IsActive && (o.ProfileId == profile.Id || _dbContext.WalletTransactions.Any(wt => wt.OrderId == o.Id && wt.ProfileId == profile.Id)))
            .OrderByDescending(o => o.CreatedAt);

        if (checkProcessingOnly)
        {
            var hasProcessing = await ordersQuery.AnyAsync(o => o.OrderStatus == OrderStatusEnum.Pending || o.OrderStatus == OrderStatusEnum.Processing || o.OrderStatus == OrderStatusEnum.Shipped || o.OrderStatus == OrderStatusEnum.Delivered, cancellationToken);
            return new PagingResponse<OrderResponse> { Items = new List<OrderResponse>(), HasProcessingOrders = hasProcessing, TotalCount = hasProcessing ? 1 : 0 };
        }

        var totalCount = await ordersQuery.CountAsync(cancellationToken);
        var orders = await ordersQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        var allAffiliates = await GetOrderAffiliatesAsync(orderIds, cancellationToken);
        var productIds = allAffiliates.Select(opa => opa.ProductId).Distinct().ToList();
        var allProducts = await GetProductsWithRelatedDataAsync(productIds, cancellationToken);
        var sellerSettings = await GetSellerSettingsAsync(allProducts, cancellationToken);
        var walletTransactions = await GetWalletTransactionsAsync(orderIds, cancellationToken);
        var itemTransactions = await GetItemTransactionsAsync(orderIds, cancellationToken);

        var items = orders.Select(o => MapToOrderResponse(o, allAffiliates, allProducts, sellerSettings, userId, walletTransactions, itemTransactions, profile.Id)).ToList();

        return new PagingResponse<OrderResponse>
        {
            Items = items,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            ItemIds = items.Select(i => i.Uid).ToList(),
            HasProcessingOrders = items.Any(i => i.Status == "Processing" || i.Status == "Awaiting Delivery" || i.Status == "Shipped")
        };
    }

    public async Task<PagingResponse<OrderResponse>> GetAllOrdersBySellerAsync(
        string sellerUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var ordersQuery = _dbContext.Orders
            .Include(o => o.Profile).ThenInclude(p => p.User)
            .Include(o => o.ShippingDetails).ThenInclude(sd => sd.CountryNavigation)
            .Include(o => o.BillingDetails).ThenInclude(bd => bd.CountryNavigation)
            .Where(o => o.IsActive && o.OrderProductAffiliates.Any(opa => opa.Product != null && opa.Product.UserId == sellerUserId))
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await ordersQuery.CountAsync(cancellationToken);
        var orders = await ordersQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        var allAffiliates = await GetOrderAffiliatesAsync(orderIds, cancellationToken);
        var productIds = allAffiliates.Select(opa => opa.ProductId).Distinct().ToList();
        var allProducts = await GetProductsWithRelatedDataAsync(productIds, cancellationToken);
        var sellerSettings = await GetSellerSettingsAsync(allProducts, cancellationToken);
        var walletTransactions = await GetWalletTransactionsAsync(orderIds, cancellationToken);
        var itemTransactions = await GetItemTransactionsAsync(orderIds, cancellationToken);

        var items = orders.Select(o => MapToOrderResponse(o, allAffiliates, allProducts, sellerSettings, sellerUserId, walletTransactions, itemTransactions)).ToList();

        return new PagingResponse<OrderResponse>
        {
            Items = items,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            ItemIds = items.Select(i => i.Uid).ToList()
        };
    }

    public async Task<OrderDetailsResponse> GetOrderDetailsAsync(string userId, string orderUid, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(o => o.ShippingDetails).ThenInclude(sd => sd.CountryNavigation)
            .Include(o => o.BillingDetails).ThenInclude(bd => bd.CountryNavigation)
            .Include(o => o.Currency)
            .Include(o => o.PaymentMethod)
            .Include(o => o.Profile).ThenInclude(p => p.User)
            .Include(o => o.OrderProductAffiliates)
            .FirstOrDefaultAsync(o => o.Uid == orderUid && o.IsActive, cancellationToken);

        if (order == null) throw new NotFoundException("Order not found");

        var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        
        var orderIds = new List<int> { order.Id };
        var allAffiliates = await GetOrderAffiliatesAsync(orderIds, cancellationToken);
        var productIds = allAffiliates.Select(opa => opa.ProductId).Distinct().ToList();
        var allProducts = await GetProductsWithRelatedDataAsync(productIds, cancellationToken);
        var sellerSettings = await GetSellerSettingsAsync(allProducts, cancellationToken);
        var walletTransactions = await GetWalletTransactionsAsync(orderIds, cancellationToken);
        var itemTransactions = await GetItemTransactionsAsync(orderIds, cancellationToken);

        return MapToOrderDetailsResponse(order, allAffiliates, allProducts, sellerSettings, userId, walletTransactions, itemTransactions, profile?.Id ?? 0);
    }

    #region Private Helpers

    private async Task<List<OrderProductAffiliate>> GetOrderAffiliatesAsync(List<int> orderIds, CancellationToken cancellationToken)
    {
        return await _dbContext.OrderProductAffiliates
            .Include(opa => opa.Product)
            .Include(opa => opa.ProductVariantCombination).ThenInclude(pvc => pvc.CombinationOptions).ThenInclude(co => co.ProductVariantOption).ThenInclude(pvo => pvo.ProductVariant)
            .Where(opa => orderIds.Contains(opa.OrderId))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<Product>> GetProductsWithRelatedDataAsync(List<int> productIds, CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .Include(p => p.Store)
            .Include(p => p.ProductMediaFiles).ThenInclude(pmf => pmf.MediaFile)
            .Include(p => p.ProductVariantCombinations).ThenInclude(pvc => pvc.CombinationOptions).ThenInclude(co => co.ProductVariantOption).ThenInclude(pvo => pvo.ProductVariant)
            .Include(p => p.Country)
            .Include(p => p.User).ThenInclude(u => u.Profile)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<dynamic>> GetSellerSettingsAsync(List<Product> products, CancellationToken cancellationToken)
    {
        var sellerUserIds = products.Where(p => !string.IsNullOrEmpty(p.UserId)).Select(p => p.UserId).Distinct().ToList();
        if (!sellerUserIds.Any()) return new List<dynamic>();

        var settings = await _dbContext.SellerSettings
            .Where(ss => sellerUserIds.Contains(ss.UserId))
            .Select(ss => new { UserId = ss.UserId, DeliveryTime = ss.DeliveryTime, ShippingCosts = ss.ShippingCosts })
            .ToListAsync(cancellationToken);
        
        return settings.Select(s => (dynamic)s).ToList();
    }

    private async Task<Dictionary<int, WalletTransaction>> GetWalletTransactionsAsync(List<int> orderIds, CancellationToken cancellationToken)
    {
        var walletTransactions = await _dbContext.WalletTransactions
            .Where(wt => orderIds.Contains(wt.OrderId ?? 0) && wt.OrderId.HasValue)
            .ToListAsync(cancellationToken);

        // Prioritize Refund transactions over Purchase transactions
        // This ensures TransactionDate shows the refund date when an order is refunded
        return walletTransactions.GroupBy(wt => wt.OrderId.Value).ToDictionary(
            g => g.Key,
            g => g.OrderByDescending(wt => wt.TransactionType == TransactionTypeEnum.Refund).ThenBy(wt => wt.TransactionDate).FirstOrDefault());
    }

    private async Task<Dictionary<int, WalletTransaction>> GetItemTransactionsAsync(List<int> orderIds, CancellationToken cancellationToken)
    {
        // Get item-level transactions (refunds) indexed by OrderProductAffiliateId
        return await _dbContext.WalletTransactions
            .Where(wt => orderIds.Contains(wt.OrderId ?? 0) && wt.OrderProductAffiliateId.HasValue)
            .ToDictionaryAsync(wt => wt.OrderProductAffiliateId.Value, wt => wt, cancellationToken);
    }

    private OrderResponse MapToOrderResponse(
        Order order,
        List<OrderProductAffiliate> allAffiliates,
        List<Product> allProducts,
        List<dynamic> sellerSettings,
        string currentUserId,
        Dictionary<int, WalletTransaction> walletTransactions,
        Dictionary<int, WalletTransaction> itemTransactions,
        int? viewerProfileId = null)
    {
        var walletTransaction = walletTransactions.ContainsKey(order.Id) ? walletTransactions[order.Id] : null;
        var payment = new OrderPaymentResponse();
        if (walletTransaction != null)
        {
            if (!string.IsNullOrEmpty(walletTransaction.CardNumberLast4) && int.TryParse(walletTransaction.CardNumberLast4, out int last4)) payment.Last4 = last4;
            if (!string.IsNullOrEmpty(walletTransaction.CardType)) payment.CardType = walletTransaction.CardType;
        }

        bool isBuyer = order.ProfileId == viewerProfileId;

        // SELLER DATA ISOLATION: 
        // If viewer is NOT the buyer, they must be a seller for this order.
        // Filter OrderItems to only show those belonging to the requesting seller.
        var orderItems = allAffiliates.Where(opa => opa.OrderId == order.Id);
        if (!isBuyer)
        {
            orderItems = orderItems.Where(opa => opa.Product != null && opa.Product.UserId == currentUserId);
        }

        // Compute effective parent status from child items (no background job dependency)
        var effectiveOrderStatus = order.OrderStatus;
        if (effectiveOrderStatus == OrderStatusEnum.Processing && orderItems.Any())
        {
            var allChildTerminal = orderItems.All(opa =>
                opa.OrderItemStatus == OrderStatusEnum.OrderFailed ||
                opa.OrderItemStatus == OrderStatusEnum.Refunded ||
                (opa.OrderItemStatus == OrderStatusEnum.Processing &&
                 opa.CountdownExpiryDate.HasValue &&
                 opa.CountdownExpiryDate.Value < DateTime.UtcNow));
            if (allChildTerminal) effectiveOrderStatus = OrderStatusEnum.OrderFailed;
        }

        string status = effectiveOrderStatus switch
        {
            OrderStatusEnum.Pending => "Processing",
            OrderStatusEnum.Processing => "Awaiting Delivery",
            OrderStatusEnum.Shipped => "Shipped",
            OrderStatusEnum.Completed => "Completed",
            OrderStatusEnum.Rejected => "Canceled",
            OrderStatusEnum.OrderFailed => "OrderFailed",
            OrderStatusEnum.Refunded => "Refunded",
            _ => "Processing"
        };

        return new OrderResponse
        {
            BuyerFullName = order.Profile?.User?.FirstName,
            SellerFullNames = orderItems.Select(opa => opa.Product?.User?.FirstName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList(),
            Status = status,
            IsProcessing = order.OrderStatus != OrderStatusEnum.Completed && order.OrderStatus != OrderStatusEnum.Rejected,
            Uid = order.Uid,
            ProfileUid = order.Profile?.Uid ?? string.Empty,
            Payment = payment,
            ShippingDetails = ShippingDetailsResponse.MapFromEntity(order.ShippingDetails) ?? new ShippingDetailsResponse(),
            BillingDetails = ShippingDetailsResponse.MapFromEntity(order.BillingDetails) ?? ShippingDetailsResponse.MapFromEntity(order.ShippingDetails) ?? new ShippingDetailsResponse(),
            Amount = order.Amount,
            TrackingNumber = order.TrackingNumber,
            ShippingProvider = order.ShippingProvider,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            PlacementDate = order.CreatedAt,
            TransactionDate = walletTransaction?.TransactionDate,
            OrderProducts = orderItems.Select(opa => MapToOrderProductDto(order, opa, allProducts, sellerSettings, currentUserId, walletTransaction, itemTransactions)).ToList()
        };
    }

    private OrderDetailsResponse MapToOrderDetailsResponse(
        Order order,
        List<OrderProductAffiliate> allAffiliates,
        List<Product> allProducts,
        List<dynamic> sellerSettings,
        string currentUserId,
        Dictionary<int, WalletTransaction> walletTransactions,
        Dictionary<int, WalletTransaction> itemTransactions,
        int viewerProfileId)
    {
        bool isBuyer = order.ProfileId == viewerProfileId;

        // SELLER DATA ISOLATION
        var orderItems = allAffiliates.Where(opa => opa.OrderId == order.Id);
        if (!isBuyer)
        {
            orderItems = orderItems.Where(opa => opa.Product != null && opa.Product.UserId == currentUserId);
        }

        return new OrderDetailsResponse
        {
            Uid = order.Uid,
            BuyerFullName = order.Profile?.User?.FirstName,
            SellerFullNames = orderItems.Select(opa => opa.Product?.User?.FirstName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList(),
            Amount = order.Amount,
            CreatedAt = order.CreatedAt,
            Note = order.Note,
            PaymentMethodUid = order.PaymentMethod?.Uid,
            ProfileUid = order.Profile?.Uid,
            StripePaymentMethodId = order.StripePaymentMethodId,
            TrackingNumber = order.TrackingNumber,
            ShippingProvider = order.ShippingProvider,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            Currency = order.Currency != null ? new CurrencyDetailsResponse { Uid = order.Currency.Uid, Code = order.Currency.Code, Name = order.Currency.Name } : null,
            ShippingDetails = ShippingDetailsResponse.MapFromEntity(order.ShippingDetails),
            BillingDetails = ShippingDetailsResponse.MapFromEntity(order.BillingDetails),
            OrderProducts = orderItems.Select(opa => MapToOrderProductDto(order, opa, allProducts, sellerSettings, currentUserId, walletTransactions.ContainsKey(order.Id) ? walletTransactions[order.Id] : null, itemTransactions)).ToList()
        };
    }

    private OrderProductResponseDto MapToOrderProductDto(
        Order order,
        OrderProductAffiliate opa,
        List<Product> allProducts,
        List<dynamic> sellerSettings,
        string currentUserId,
        WalletTransaction walletTransaction,
        Dictionary<int, WalletTransaction> itemTransactions)
    {
        var product = allProducts.FirstOrDefault(p => p.Id == opa.ProductId);
        var isProductDeleted = product == null || !product.IsActive;

        string deliveryTime = opa.DeliveryTimeSnapshot ?? "7 days";
        decimal shippingCost = 0;
        string productName, productDescription, primaryImageUrl, brand, countryCode, currencyCode;
        decimal? productPrice;
        double? minPrice, maxPrice;
        ProductTypeEnum productType;
        ProfileBaseResponse profile;
        List<string> variantTypes;
        var mediaFiles = new List<MediaFileDetailsResponse>();

        if (isProductDeleted)
        {
            productName = opa.ProductNameSnapshot ?? string.Empty;
            productDescription = opa.ProductDescriptionSnapshot ?? string.Empty;
            productPrice = opa.ProductPriceSnapshot;
            primaryImageUrl = opa.PrimaryImageUrlSnapshot;
            brand = opa.ProductBrandSnapshot;
            minPrice = opa.ProductMinPriceSnapshot;
            maxPrice = opa.ProductMaxPriceSnapshot;
            countryCode = opa.CountryCodeSnapshot;
            currencyCode = opa.CurrencyCodeSnapshot;
            productType = (ProductTypeEnum)opa.ProductTypeSnapshot;
            shippingCost = opa.ShippingCostSnapshot ?? 0;
            deliveryTime = opa.DeliveryTimeSnapshot ?? "7 days";
            if (!string.IsNullOrWhiteSpace(opa.PrimaryImageUrlSnapshot)) mediaFiles.Add(new MediaFileDetailsResponse { Url = opa.PrimaryImageUrlSnapshot, FileType = "Image" });
            profile = !string.IsNullOrWhiteSpace(opa.ProfileUidSnapshot) ? new ProfileBaseResponse { Uid = opa.ProfileUidSnapshot, Username = opa.ProfileUsernameSnapshot } : null;
            variantTypes = !string.IsNullOrWhiteSpace(opa.VariantTypesSnapshot) ? JsonConvert.DeserializeObject<List<string>>(opa.VariantTypesSnapshot) ?? new List<string>() : new List<string>();
        }
        else
        {
            productName = product.Name ?? string.Empty;
            productDescription = string.IsNullOrWhiteSpace(product.ProductDetail) ? product.WhatIsIt ?? string.Empty : $"{product.WhatIsIt ?? string.Empty} {product.ProductDetail}".Trim();
            productPrice = opa.ProductPriceSnapshot;
            primaryImageUrl = product.ProductMediaFiles?.Where(pmf => pmf.MediaFile != null && pmf.MediaFile.IsActive).OrderBy(pmf => pmf.MediaFile.Priority).FirstOrDefault()?.MediaFile.Url;
            brand = product.Brand;
            minPrice = product.MinPrice;
            maxPrice = product.MaxPrice;
            countryCode = product.Country?.Iso2;
            currencyCode = product.Country?.Iso4;
            productType = product.Type;

            var sellerUserId = product?.UserId ?? product?.Store?.UserId;
            if (!string.IsNullOrEmpty(sellerUserId))
            {
                var settings = sellerSettings.FirstOrDefault(ss => ss.UserId == sellerUserId);
                if (settings != null)
                {
                    // Use snapshot for delivery time (seller changes must not affect existing orders)
                    if (string.IsNullOrWhiteSpace(deliveryTime)) deliveryTime = settings.DeliveryTime ?? "7 days";
                    shippingCost = settings.ShippingCosts ?? 0;
                }
            }

            mediaFiles = product.ProductMediaFiles?.Where(pmf => pmf.MediaFile != null && pmf.MediaFile.IsActive).Select(pmf => new MediaFileDetailsResponse 
            { 
                Url = pmf.MediaFile.Url, FileType = pmf.MediaFile.MediaFileType.ToString(), Uid = pmf.MediaFile.Uid, Priority = pmf.MediaFile.Priority,
                IsHlsProcessed = pmf.MediaFile.IsHlsProcessed, OriginalUrl = pmf.MediaFile.OriginalUrl, HlsBasePath = pmf.MediaFile.HlsBasePath,
                VideoDurationSeconds = pmf.MediaFile.VideoDurationSeconds, AvailableQualities = pmf.MediaFile.AvailableQualities
            }).ToList() ?? new List<MediaFileDetailsResponse>();

            profile = product?.User?.Profile != null ? new ProfileBaseResponse { Uid = product.User.Profile.Uid, Username = product.User?.UserName, FullName = product.User?.FirstName } : null;

            variantTypes = new List<string>();
            var combination = opa.ProductVariantCombination ?? product.ProductVariantCombinations?.FirstOrDefault(pvc => pvc.Id == opa.ProductVariantCombinationId) ?? product.ProductVariantCombinations?.FirstOrDefault(pvc => pvc.IsActive);
            if (combination?.CombinationOptions != null)
            {
                variantTypes = combination.CombinationOptions.OrderBy(co => co.ProductVariantOption?.ProductVariant?.Id ?? 0).Select(co => co.ProductVariantOption?.Value).Where(v => !string.IsNullOrEmpty(v)).ToList();
                if (!string.IsNullOrWhiteSpace(combination.ImageUrl)) primaryImageUrl = combination.ImageUrl;
            }
        }

        var orderType = !isProductDeleted && product != null && product.UserId == currentUserId ? "Sale" : "Purchase";
        var deliveryDays = ParseDeliveryDays(deliveryTime);

        // Fallback: if CountdownExpiryDate was never saved (old orders), compute from CreatedAt + snapshot
        var effectiveCountdownExpiry = opa.CountdownExpiryDate
            ?? (deliveryDays.HasValue ? order.CreatedAt.AddDays(deliveryDays.Value) : order.CreatedAt.AddDays(7));
        var isCountdownExpired = effectiveCountdownExpiry < DateTime.UtcNow;

        // Extension tracking for shipped items
        var isExtensionExpired = opa.ExtensionExpiryDate.HasValue && opa.ExtensionExpiryDate.Value < DateTime.UtcNow;
        var hasExtensionAvailable = opa.ExtensionCount == 0;  // Can extend only once

        // Calculate countdown label for UI
        string countdownLabel = "Active";
        if (opa.OrderItemStatus == OrderStatusEnum.Shipped)
        {
            if (isExtensionExpired && opa.ExtensionCount > 0) countdownLabel = "Extension Over";
            else if (opa.ExtensionExpiryDate.HasValue && opa.ExtensionExpiryDate > DateTime.UtcNow) countdownLabel = "Active";
            else if (isCountdownExpired) countdownLabel = "Awaiting Confirmation";
        }
        else if (isCountdownExpired && opa.OrderItemStatus == OrderStatusEnum.Processing)
        {
            countdownLabel = "Expired";
        }

        // For shipped items, determine effective status considering extensions
        var effectiveStatus = opa.OrderItemStatus;
        if (opa.OrderItemStatus == OrderStatusEnum.Shipped && isCountdownExpired)
        {
            // If countdown expired but extension is active, keep as Shipped
            if (opa.ExtensionExpiryDate.HasValue && opa.ExtensionExpiryDate > DateTime.UtcNow)
            {
                effectiveStatus = OrderStatusEnum.Shipped;
            }
        }

        // Item-level transaction (refund) takes priority, fallback to order-level (purchase)
        var itemTransaction = itemTransactions.ContainsKey(opa.Id) ? itemTransactions[opa.Id] : null;
        var transactionDate = itemTransaction?.TransactionDate ?? walletTransaction?.TransactionDate;

        return new OrderProductResponseDto
        {
            OrderItemId = opa.Id,
            OrderUid = order.Uid,
            ProductOrderUid = opa.Uid,
            OrderType = orderType,
            DeliveryWithin = deliveryDays.HasValue ? order.CreatedAt.AddDays(deliveryDays.Value) : order.CreatedAt.AddDays(7),
            PlacementDate = order.CreatedAt,
            TransactionDate = transactionDate,
            // Item-level status tracking
            OrderItemStatus = (isCountdownExpired && opa.OrderItemStatus == OrderStatusEnum.Processing)
                ? OrderStatusEnum.OrderFailed
                : effectiveStatus,
            TrackingNumber = opa.TrackingNumber,
            ShippingProvider = opa.ShippingProvider,
            ShippedAt = opa.ShippedAt,
            DeliveredAt = opa.DeliveredAt,
            // Retry/Reorder tracking
            RetryCount = opa.RetryCount,
            CountdownExpiryDate = effectiveCountdownExpiry,
            NewCountdownExpiryDate = opa.NewCountdownExpiryDate,
            IsRetryAllowed = opa.IsRetryAllowed,
            IsCountdownExpired = isCountdownExpired,
            // Delivery extension tracking
            ExtensionCount = opa.ExtensionCount,
            ExtensionExpiryDate = opa.ExtensionExpiryDate,
            IsExtensionExpired = isExtensionExpired,
            CountdownLabel = countdownLabel,
            // Action flags for buyer — eligible if OrderFailed OR countdown expired while still Processing
            CanRefund = opa.OrderItemStatus == OrderStatusEnum.OrderFailed || (isCountdownExpired && opa.OrderItemStatus == OrderStatusEnum.Processing),
            CanReorder = (opa.OrderItemStatus == OrderStatusEnum.OrderFailed || (isCountdownExpired && opa.OrderItemStatus == OrderStatusEnum.Processing)) && opa.RetryCount < 1 && opa.IsRetryAllowed,
            // Extension flags for shipped items
            CanExtend = opa.OrderItemStatus == OrderStatusEnum.Shipped && isCountdownExpired && hasExtensionAvailable && !isExtensionExpired,
            CanReportIssue = opa.OrderItemStatus == OrderStatusEnum.OrderFailed && opa.ExtensionCount > 0 && isExtensionExpired,
            Product = new OrderProductDetailsResponse
            {
                ProductUid = isProductDeleted ? string.Empty : (product?.Uid ?? string.Empty),
                ProductVariantCombinationUid = opa.ProductVariantCombinationUidSnapshot,
                Name = productName, Brand = brand, MinPrice = minPrice, MaxPrice = maxPrice, Price = productPrice, BagQuantity = opa.ProductQuantity,
                ShippingCost = shippingCost, DeliveryTime = deliveryTime, ImageUrl = primaryImageUrl, CountryCode = countryCode, CurrencyCode = currencyCode,
                Type = productType, ProductMediaFiles = mediaFiles, Profile = profile, VarinatTypes = variantTypes
            }
        };
    }

    private double? ParseDeliveryDays(string deliveryTime)
    {
        if (string.IsNullOrWhiteSpace(deliveryTime)) return null;
        deliveryTime = deliveryTime.ToLower().Trim();
        var rangeMatch = System.Text.RegularExpressions.Regex.Match(deliveryTime, @"(\d+)\s*-\s*(\d+)\s*(day|week)s?");
        if (rangeMatch.Success) return rangeMatch.Groups[3].Value == "week" ? int.Parse(rangeMatch.Groups[2].Value) * 7 : int.Parse(rangeMatch.Groups[2].Value);
        var singleMatch = System.Text.RegularExpressions.Regex.Match(deliveryTime, @"(\d+)\s*(day|week)s?");
        if (singleMatch.Success) return singleMatch.Groups[2].Value == "week" ? int.Parse(singleMatch.Groups[1].Value) * 7 : int.Parse(singleMatch.Groups[1].Value);
        var minuteMatch = System.Text.RegularExpressions.Regex.Match(deliveryTime, @"(\d+)\s*(minute|min)s?");
        if (minuteMatch.Success) return double.Parse(minuteMatch.Groups[1].Value) / 1440.0;
        var hourMatch = System.Text.RegularExpressions.Regex.Match(deliveryTime, @"(\d+)\s*hours?");
        if (hourMatch.Success) return double.Parse(hourMatch.Groups[1].Value) / 24.0;
        var numberMatch = System.Text.RegularExpressions.Regex.Match(deliveryTime, @"(\d+)");
        return numberMatch.Success ? int.Parse(numberMatch.Groups[1].Value) : null;
    }

    #endregion

    #region Granular Order Item Status Updates

    /// <summary>
    /// Validates that the seller owns the specified order item.
    /// This is a reusable guard method to ensure data isolation.
    /// </summary>
    public async Task<OrderProductAffiliate> ValidateSellerOwnershipAsync(string sellerUserId, string itemUid, CancellationToken cancellationToken)
    {
        var orderItem = await _dbContext.OrderProductAffiliates
            .Include(opa => opa.Product)
            .Include(opa => opa.Order)
            .FirstOrDefaultAsync(opa => opa.Uid == itemUid && opa.IsActive, cancellationToken);

        if (orderItem == null)
        {
            throw new NotFoundException($"Order item with UID {itemUid} not found.");
        }

        if (orderItem.Product == null || orderItem.Product.UserId != sellerUserId)
        {
            throw new ForbiddenException("You are not authorized to update this order item.");
        }

        return orderItem;
    }

    /// <summary>
    /// Updates the status of multiple order items to Shipped.
    /// Automatically updates the parent order status based on all items' statuses.
    /// </summary>
    public async Task<bool> UpdateOrderItemsStatusAsync(
        string sellerUserId, 
        List<string> itemUids, 
        string trackingNumber, 
        string shippingProvider, 
        CancellationToken cancellationToken)
    {
        var orderItems = await _dbContext.OrderProductAffiliates
            .Include(opa => opa.Product)
            .Include(opa => opa.Order)
            .Where(opa => itemUids.Contains(opa.Uid) && opa.IsActive)
            .ToListAsync(cancellationToken);

        if (!orderItems.Any())
        {
            throw new NotFoundException($"No active order items found for the provided UIDs.");
        }

        var orderIdsToUpdate = new HashSet<int>();

        foreach (var orderItem in orderItems)
        {
            // Validate ownership
            if (orderItem.Product == null || orderItem.Product.UserId != sellerUserId)
            {
                throw new ForbiddenException($"You are not authorized to update order item {orderItem.Uid}.");
            }

            // Verify the item is in Approved status
            if (orderItem.OrderItemStatus != OrderStatusEnum.Processing)
            {
                throw new BadRequestException($"Order item {orderItem.Uid} cannot be marked as shipped. Current status: {orderItem.OrderItemStatus}");
            }

            // Update the order item
            orderItem.OrderItemStatus = OrderStatusEnum.Shipped;
            orderItem.TrackingNumber = trackingNumber;
            orderItem.ShippingProvider = shippingProvider;
            orderItem.ShippedAt = DateTime.UtcNow;
            orderItem.UpdatedAt = DateTime.UtcNow;

            orderIdsToUpdate.Add(orderItem.OrderId);
        }

        // Update parent order statuses
        foreach (var orderId in orderIdsToUpdate)
        {
            await UpdateParentOrderStatusAsync(orderId, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send email notification to buyers - one email per order with all shipped items
        foreach (var orderId in orderIdsToUpdate)
        {
            try
            {
                // Get the order with all necessary related data
                var order = await _dbContext.Orders
                    .Include(o => o.Profile).ThenInclude(p => p.User)
                    .Include(o => o.ShippingDetails).ThenInclude(sd => sd.CountryNavigation)
                    .Include(o => o.Currency)
                    .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

                if (order != null)
                {
                    // Get all shipped items for this order that were just marked as shipped
                    var shippedItemsForOrder = orderItems
                        .Where(oi => oi.OrderId == orderId)
                        .ToList();

                    // Load product details for the shipped items
                    var productIds = shippedItemsForOrder.Select(oi => oi.ProductId).Distinct().ToList();
                    var products = await _dbContext.Products
                        .Include(p => p.ProductMediaFiles).ThenInclude(pmf => pmf.MediaFile)
                        .Where(p => productIds.Contains(p.Id))
                        .ToListAsync(cancellationToken);

                    // Attach product data to order items for email
                    foreach (var item in shippedItemsForOrder)
                    {
                        if (item.Product == null)
                        {
                            item.Product = products.FirstOrDefault(p => p.Id == item.ProductId);
                        }
                    }

                    await _emailService.SendOrderShippedEmailAsync(order, shippedItemsForOrder, trackingNumber, shippingProvider);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order shipped email for order ID {OrderId}", orderId);
                // Continue processing other orders even if email fails
            }
        }

        return true;
    }

    /// <summary>
    /// Confirms delivery of multiple order items.
    /// Automatically updates the parent order status based on all items' statuses.
    /// </summary>
    public async Task<bool> ConfirmOrderItemsDeliveryAsync(
        string buyerUserId, 
        List<string> itemUids, 
        CancellationToken cancellationToken)
    {
        var orderItems = await _dbContext.OrderProductAffiliates
            .Include(opa => opa.Order)
                .ThenInclude(o => o.Profile)
            .Where(opa => itemUids.Contains(opa.Uid) && opa.IsActive)
            .ToListAsync(cancellationToken);

        if (!orderItems.Any())
        {
            throw new NotFoundException($"No active order items found for the provided UIDs.");
        }

        var orderIdsToUpdate = new HashSet<int>();

        foreach (var orderItem in orderItems)
        {
            // Verify the current user is the buyer
            if (orderItem.Order.Profile.UserId != buyerUserId)
            {
                throw new ForbiddenException($"You are not authorized to confirm delivery for order item {orderItem.Uid}.");
            }

            // Verify the item is in Shipped status
            if (orderItem.OrderItemStatus != OrderStatusEnum.Shipped)
            {
                throw new BadRequestException($"Order item {orderItem.Uid} cannot be confirmed as delivered. Current status: {orderItem.OrderItemStatus}");
            }

            // Update the order item
            orderItem.OrderItemStatus = OrderStatusEnum.Delivered;
            orderItem.DeliveredAt = DateTime.UtcNow;
            orderItem.UpdatedAt = DateTime.UtcNow;

            orderIdsToUpdate.Add(orderItem.OrderId);
        }

        // Update parent order statuses
        foreach (var orderId in orderIdsToUpdate)
        {
            await UpdateParentOrderStatusAsync(orderId, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Updates the parent order status based on the statuses of all its items.
    /// Logic:
    /// - If all items are Delivered -> Order is Completed
    /// - If all items are Shipped or Delivered -> Order is Shipped (Fully Shipped)
    /// - If some items are Shipped/Delivered and others are not -> Order is PartiallyShipped
    /// - Otherwise, keep current status
    /// </summary>
    private async Task UpdateParentOrderStatusAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(o => o.OrderProductAffiliates)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null) return;

        var allItems = order.OrderProductAffiliates.Where(opa => opa.IsActive).ToList();
        if (!allItems.Any()) return;

        var allDelivered = allItems.All(item => item.OrderItemStatus == OrderStatusEnum.Delivered);
        var allShippedOrDelivered = allItems.All(item => 
            item.OrderItemStatus == OrderStatusEnum.Shipped || 
            item.OrderItemStatus == OrderStatusEnum.Delivered);
        var anyShippedOrDelivered = allItems.Any(item => 
            item.OrderItemStatus == OrderStatusEnum.Shipped || 
            item.OrderItemStatus == OrderStatusEnum.Delivered);

        if (allDelivered)
        {
            order.OrderStatus = OrderStatusEnum.Completed;
            order.DeliveredAt = DateTime.UtcNow;
        }
        else if (allShippedOrDelivered)
        {
            order.OrderStatus = OrderStatusEnum.Shipped;
            if (order.ShippedAt == null)
            {
                order.ShippedAt = DateTime.UtcNow;
            }
        }
        else if (anyShippedOrDelivered)
        {
            // Partially shipped - we'll use a custom status or keep as Approved
            // For now, we'll keep it as Approved but you may want to add PartiallyShipped to the enum
            order.OrderStatus = OrderStatusEnum.Processing;
        }

        order.UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}

