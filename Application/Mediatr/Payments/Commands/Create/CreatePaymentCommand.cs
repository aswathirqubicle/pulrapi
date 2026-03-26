using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Mediatr.ShippingDetails.Queries;
using Core.Application.Models.Orders;
using Core.Application.Models.Products;
using Core.Application.Models.ShippingDetails;
using Core.Application.Models.Stripe;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Models.Wallet;

namespace Core.Application.Mediatr.Payments.Commands.Create;

public class CreatePaymentCommand : IRequest<CreatePaymentResponse>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string? PaymentMethodId { get; set; }
    public string? Note { get; set; }
    public List<CheckoutProductRequest> Products { get; set; } = new();
    public string? ShippingDetailsUid { get; set; }
    
    /// <summary>
    /// Optional billing address UID. If null, uses the default billing address or falls back to shipping address.
    /// </summary>
    public string? BillingDetailsUid { get; set; }

    /// <summary>
    /// Required for 3D Secure redirect flows when Confirm = true.
    /// </summary>
    public string? ReturnUrl { get; set; }
}


public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    private readonly IStripeService _stripeService;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CreatePaymentCommandHandler> _logger;
    private readonly IEmailService _emailService;
    private readonly IOrderService _orderService;

    public CreatePaymentCommandHandler(
        IStripeService stripeService,
        IMediator mediator,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext,
        ILogger<CreatePaymentCommandHandler> logger,
        IEmailService emailService,
        IOrderService orderService)
    {
        _stripeService = stripeService;
        _mediator = mediator;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _logger = logger;
        _emailService = emailService;
        _orderService = orderService;
    }


    public async Task<CreatePaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        CheckoutSummaryResponse? checkoutSummary = null;

        try
        {
            checkoutSummary = await BuildCheckoutSummaryAsync(request, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            return new CreatePaymentResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return new CreatePaymentResponse
            {
                Success = false,
                Error = $"Authentication required: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            var errorMessage = ex.Message;
            if (errorMessage.Contains("user roles") || errorMessage.Contains("Error getting user") ||
                errorMessage.Contains("GetUserAsync") || errorMessage.Contains("GetRoles"))
            {
                errorMessage = "Authentication error: Unable to verify user identity. Please log in again or refresh your session.";
            }

            return new CreatePaymentResponse
            {
                Success = false,
                Error = $"Failed to build checkout summary: {errorMessage}"
            };
        }

        try
        {
            var paymentRequest = new CreatePaymentRequest
            {
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethodId = request.PaymentMethodId,
                Note = request.Note,
                Products = request.Products,
                ShippingDetailsUid = request.ShippingDetailsUid,
                OrderId = checkoutSummary.OrderId,
                ReturnUrl = request.ReturnUrl
            };

            var paymentResponse = await _stripeService.CreatePaymentAsync(paymentRequest);

            if (!string.IsNullOrWhiteSpace(request.PaymentMethodId))
            {
                var saveResult = await SaveOrderToDatabaseAsync(request, checkoutSummary, paymentResponse, cancellationToken);
                if (!saveResult.Success)
                {
                    _logger.LogError("Failed to save order: {Error}", saveResult.Error);
                }
                else
                {
                    paymentResponse.WalletTransaction = saveResult.WalletTransaction;
                }
            }

            paymentResponse.Success = true;
            paymentResponse.Error = null;
            paymentResponse.CheckoutSummary = checkoutSummary;
            paymentResponse.DeliveryTime = checkoutSummary?.DeliveryTime;
            paymentResponse.TotalShippingCost = checkoutSummary?.TotalShippingCost ?? 0;
            paymentResponse.TotalProductCost = checkoutSummary?.TotalProductCost ?? 0;

            // Add full order details with item-level statuses if order was saved
            if (!string.IsNullOrWhiteSpace(checkoutSummary?.OrderId))
            {
                try
                {
                    var user = await _currentUserService.GetUserAsync(skipDetails: true);
                    if (user != null)
                    {
                        paymentResponse.OrderDetails = await _orderService.GetOrderDetailsAsync(
                            user.Id, 
                            checkoutSummary.OrderId, 
                            cancellationToken);
                    }
                }
                catch (Exception orderEx)
                {
                    _logger.LogWarning(orderEx, "Failed to fetch order details for payment response");
                    // Don't fail the payment if we can't fetch order details
                }
            }

            return paymentResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment: {Message}", ex.Message);
            
            return new CreatePaymentResponse
            {
                Success = false,
                Error = ex.Message,
                CheckoutSummary = checkoutSummary,
                DeliveryTime = checkoutSummary?.DeliveryTime,
                TotalShippingCost = checkoutSummary?.TotalShippingCost ?? 0,
                TotalProductCost = checkoutSummary?.TotalProductCost ?? 0
            };
        }
    }

    private async Task<CheckoutSummaryResponse> BuildCheckoutSummaryAsync(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var orderId = $"P{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
        var products = new List<CheckoutProductSummary>();
        decimal totalProductCost = 0;


        // Get all product user IDs to fetch seller settings in batch
        var productUids = request.Products.Select(p => p.ProductUid).ToList();
        var productEntities = await _dbContext.Products
            .Where(p => productUids.Contains(p.Uid))
            .Include(p => p.ProductMediaFiles)
                .ThenInclude(pmf => pmf.MediaFile)
            .Include(p => p.ProductVariant)
                .ThenInclude(pv => pv.ProductVariantOptions)
            .Include(p => p.ProductVariantCombinations)
                .ThenInclude(pvc => pvc.CombinationOptions)
                    .ThenInclude(co => co.ProductVariantOption)
                        .ThenInclude(pvo => pvo.ProductVariant)
            .Include(p => p.User)
                .ThenInclude(u => u.Profile)
            .ToListAsync(cancellationToken);

        var sellerUserIds = productEntities
            .Where(p => p.UserId != null)
            .Select(p => p.UserId)
            .Distinct()
            .ToList();

        var sellerSettingsDict = await _dbContext.SellerSettings
            .Where(ss => sellerUserIds.Contains(ss.UserId))
            .ToDictionaryAsync(ss => ss.UserId, ss => ss, cancellationToken);

        foreach (var pReq in request.Products)
        {
            var productEntity = productEntities.FirstOrDefault(p => p.Uid == pReq.ProductUid);
            if (productEntity == null)
            {
                throw new ArgumentException($"Product with uid {pReq.ProductUid} not found.");
            }

            var product = await _mediator.Send(new GetProductDetailsQuery
            {
                Uid = pReq.ProductUid,
                CurrencyCode = request.Currency
            }, cancellationToken);

            if (product == null)
            {
                throw new ArgumentException($"Product with uid {pReq.ProductUid} not found.");
            }

            decimal unitPrice = product.MinPrice.HasValue ? (decimal)product.MinPrice.Value : 0;
            ProductVariantCombinationResponse selectedCombinationResponse = null;
            List<ProductVariantResponse> productVariants = new();

            if (!string.IsNullOrWhiteSpace(pReq.VariantCombinationUid) &&
                product.ProductVariantCombinations != null &&
                product.ProductVariantCombinations.Count > 0)
            {
                var allCombinations = product.ProductVariantCombinations
                    .SelectMany(kvp => kvp.Value)
                    .ToList();

                var selectedCombination = allCombinations
                    .FirstOrDefault(vc => vc.Uid == pReq.VariantCombinationUid);

                if (selectedCombination == null)
                {
                    throw new ArgumentException($"Variant combination {pReq.VariantCombinationUid} not found for product {pReq.ProductUid}.");
                }

                unitPrice = selectedCombination.Price.HasValue ? selectedCombination.Price.Value : unitPrice;

                selectedCombinationResponse = new ProductVariantCombinationResponse
                {
                    Uid = selectedCombination.Uid,
                    SKU = selectedCombination.SKU,
                    Price = selectedCombination.Price,
                    Quantity = selectedCombination.Quantity,
                    ImageUrl = selectedCombination.ImageUrl,
                    IsAvailable = selectedCombination.IsAvailable,
                    DisplayName = selectedCombination.DisplayName,
                    VariantValues = selectedCombination.VariantValues
                };

                // Get product variants for the selected combination
                if (productEntity.ProductVariant != null && productEntity.ProductVariantCombinations != null)
                {
                    var variantCombinationEntity = productEntity.ProductVariantCombinations
                        .FirstOrDefault(vc => vc.Uid == pReq.VariantCombinationUid);

                    if (variantCombinationEntity != null && variantCombinationEntity.CombinationOptions.Any())
                    {
                        var variantIds = variantCombinationEntity.CombinationOptions
                            .Select(co => co.ProductVariantOption?.ProductVariant?.Id)
                            .Where(id => id.HasValue)
                            .Distinct()
                            .ToList();

                        productVariants = productEntity.ProductVariant
                            .Where(pv => variantIds.Contains(pv.Id))
                            .Select(pv => new ProductVariantResponse
                            {
                                VariantName = pv.VariantName,
                                VariantOptions = variantCombinationEntity.CombinationOptions
                                    .Where(co => co.ProductVariantOption?.ProductVariant?.Id == pv.Id)
                                    .Select(co => co.ProductVariantOption.Value)
                                    .ToList()
                            })
                            .ToList();
                    }
                }
            }
            else
            {
                productVariants = product.ProductVariants ?? new List<Core.Application.Models.Products.ProductVariantResponse>();
            }

            totalProductCost += unitPrice * pReq.Quantity;

            // Get seller settings for shipping cost and delivery time
            SellerSettings? sellerSettings = null;
            if (productEntity.UserId != null)
            {
                sellerSettingsDict.TryGetValue(productEntity.UserId, out sellerSettings);
            }

            // Map media files from productEntity (has Priority field)
            var productMediaFiles = productEntity.ProductMediaFiles?
                .Where(pmf => pmf.MediaFile.IsActive)
                .OrderBy(pmf => pmf.MediaFile.Priority)
                .Select(pmf => new Core.Application.Models.MediaFiles.MediaFileDetailsResponse
                {
                    Uid = pmf.MediaFile.Uid,
                    Url = pmf.MediaFile.Url,
                    FileType = pmf.MediaFile.MediaFileType.ToString(),
                    Priority = pmf.MediaFile.Priority
                })
                .ToList() ?? new List<Core.Application.Models.MediaFiles.MediaFileDetailsResponse>();

            // Map product owner's profile
            Core.Application.Models.Profiles.ProfileResponse? ownerProfile = null;
            if (productEntity.User?.Profile != null)
            {
                ownerProfile = new Core.Application.Models.Profiles.ProfileResponse
                {
                    Uid = productEntity.User.Profile.Uid,
                    ImageUrl = productEntity.User.Profile.ImageUrl ?? string.Empty,
                    FullName = productEntity.User.FirstName ?? string.Empty,
                    FirstName = productEntity.User.FirstName ?? string.Empty,
                    LastName = productEntity.User.LastName ?? string.Empty,
                    Username = productEntity.User.UserName ?? string.Empty,
                    UserId = productEntity.User.Id,
                    UserType = productEntity.User.Profile.UserType.ToString(),
                    // Note: Followers, Following, PostsCount, and other stats are not loaded here for performance
                    // They can be loaded separately if needed
                    Followers = 0,
                    Following = 0,
                    PostsCount = 0,
                    FollowedByMe = false,
                    IsInfluencer = false,
                    About = productEntity.User.Profile.About ?? string.Empty,
                    Stores = new List<Core.Application.Models.Stores.StoreDetailsResponse>()
                };
            }

            products.Add(new CheckoutProductSummary
            {
                Uid = product.Uid,
                Name = product.Name,
                Brand = product.Brand ?? string.Empty,
                ProductUrl = product.ProductUrl ?? string.Empty,
                Type = product.Type,
                ProductMediaFiles = productMediaFiles,
                ProductVariants = productVariants,
                BagQuantity = pReq.Quantity,
                ProductVariantCombinationUid = pReq.VariantCombinationUid,
                Price = unitPrice,
                ShippingCost = sellerSettings?.ShippingCosts,
                DeliveryTime = string.IsNullOrWhiteSpace(sellerSettings?.DeliveryTime) ? "7 days" : sellerSettings.DeliveryTime,
                ProductVariantCombinations = selectedCombinationResponse,
                OwnerProfile = ownerProfile
            });
        }

        ShippingDetailsResponse shippingResponse = string.IsNullOrWhiteSpace(request.ShippingDetailsUid)
            ? await _mediator.Send(new GetDefaultShippingAddressQuery { IsBillingAddress = false }, cancellationToken)
            : await _mediator.Send(new GetShippingAddressQuery { Uid = request.ShippingDetailsUid }, cancellationToken);

        // Get billing address if specified, otherwise use default billing or null (will fall back to shipping)
        ShippingDetailsResponse? billingResponse = null;
        if (!string.IsNullOrWhiteSpace(request.BillingDetailsUid))
        {
            billingResponse = await _mediator.Send(new GetShippingAddressQuery { Uid = request.BillingDetailsUid }, cancellationToken);
        }
        else
        {
            // Try to get default billing address
            try
            {
                billingResponse = await _mediator.Send(new GetDefaultShippingAddressQuery { IsBillingAddress = true }, cancellationToken);
            }
            catch
            {
                // No default billing address found, will use shipping address
                billingResponse = null;
            }
        }

        // Build payment response
        CheckoutPaymentResponse? payment = null;
        if (!string.IsNullOrWhiteSpace(request.PaymentMethodId))
        {
            var paymentMethod = await _stripeService.GetPaymentMethodAsync(request.PaymentMethodId);
            if (paymentMethod != null)
            {
                payment = new CheckoutPaymentResponse
                {
                    Brand = paymentMethod.Brand,
                    PaymentMethod = "Card"
                };
            }
            else
            {
                payment = new CheckoutPaymentResponse
                {
                    Brand = string.Empty,
                    PaymentMethod = "Card"
                };
            }
        }
        else
        {
            // No PaymentMethodId means it's cash on delivery
            payment = new CheckoutPaymentResponse
            {
                Brand = string.Empty,
                PaymentMethod = "Cash on delivery"
            };
        }

        var totalShippingCost = sellerUserIds.Sum(sellerId => 
        {
            if (sellerSettingsDict.TryGetValue(sellerId, out var ss))
            {
                return ss.ShippingCosts ?? 0;
            }
            return 0; // Default shipping cost is 0 if seller hasn't set it
        });

        return new CheckoutSummaryResponse
        {
            OrderId = orderId,
            Amount = request.Amount,
            Currency = request.Currency,
            TotalProducts = products.Count,
            TotalShippingCost = totalShippingCost,
            TotalProductCost = totalProductCost,
            Products = products,
            Payment = payment,
            ShippingDetails = shippingResponse,
            BillingDetails = billingResponse,
            Note = request.Note,
            DeliveryTime = products.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.DeliveryTime))?.DeliveryTime ?? "7 days"
        };
    }

    private class SaveOrderResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public WalletTransactionResponse? WalletTransaction { get; set; }
    }

    private async Task<SaveOrderResult> SaveOrderToDatabaseAsync(
        CreatePaymentCommand request,
        CheckoutSummaryResponse checkoutSummary,
        CreatePaymentResponse paymentResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
            {
                return new SaveOrderResult { Success = false, Error = "User is null" };
            }

            var profile = await _dbContext.Profiles
                .Include(p => p.User)
                .ThenInclude(u => u.ShippingDetails)
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            if (profile == null)
            {
                return new SaveOrderResult { Success = false, Error = $"Profile not found for user {user.Id}" };
            }

            if (profile.User.ShippingDetails == null || !profile.User.ShippingDetails.Any())
            {
                return new SaveOrderResult { Success = false, Error = $"No shipping details found for user {user.Id}" };
            }

            var currency = await _dbContext.Currencies
                .FirstOrDefaultAsync(c => c.Code.ToLower() == request.Currency.ToLower(), cancellationToken);

            if (currency == null)
            {
                return new SaveOrderResult { Success = false, Error = $"Currency not found: {request.Currency}" };
            }

            // Determine payment method based on whether PaymentMethodId is provided
            PaymentMethodEnum paymentMethodEnum = !string.IsNullOrWhiteSpace(request.PaymentMethodId)
                ? PaymentMethodEnum.CreditCard
                : PaymentMethodEnum.CashOnDelivery;

            var paymentMethod = await _dbContext.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.Key == paymentMethodEnum.ToString(), cancellationToken);

            if (paymentMethod == null)
            {
                return new SaveOrderResult { Success = false, Error = $"PaymentMethod not found for {paymentMethodEnum}" };
            }


            var shippingDetails = profile.User.ShippingDetails
                .FirstOrDefault(sd => sd.Uid == checkoutSummary.ShippingDetails?.Uid)
                ?? profile.User.ShippingDetails.FirstOrDefault(sd => sd.DefaultShippingAddress && !sd.IsBillingAddress);

            if (shippingDetails == null)
            {
                return new SaveOrderResult { Success = false, Error = "Shipping details not found" };
            }

            // Get billing address if provided in checkout summary
            Core.Domain.Entities.ShippingDetails? billingDetails = null;
            if (checkoutSummary.BillingDetails != null)
            {
                billingDetails = profile.User.ShippingDetails
                    .FirstOrDefault(sd => sd.Uid == checkoutSummary.BillingDetails.Uid);
            }

            var orderProductAffiliates = new List<OrderProductAffiliate>();
            var variantCombinationsToUpdate = new List<(ProductVariantCombination variantCombination, int quantityToDecrease)>();

            foreach (var productSummary in checkoutSummary.Products)
            {
                // Load product with all related data to capture complete snapshot
                var product = await _dbContext.Products
                    .Include(p => p.ProductMediaFiles)
                        .ThenInclude(pmf => pmf.MediaFile)
                    .Include(p => p.ProductVariantCombinations)
                        .ThenInclude(pvc => pvc.CombinationOptions)
                            .ThenInclude(co => co.ProductVariantOption)
                                .ThenInclude(pvo => pvo.ProductVariant)
                    .Include(p => p.Country)
                    .Include(p => p.User)
                        .ThenInclude(u => u.Profile)
                    .FirstOrDefaultAsync(p => p.Uid == productSummary.Uid, cancellationToken);

                if (product == null)
                {
                    continue;
                }

                // Prevent users from purchasing their own products
                if (product.UserId == user.Id)
                {
                    return new SaveOrderResult
                    {
                        Success = false,
                        Error = "You cannot purchase your own product."
                    };
                }

                // Remove the product from the user's bag
                var bagItem = await _dbContext.UserBagProducts
                    .FirstOrDefaultAsync(ubp => ubp.UserId == user.Id && 
                                               ubp.BagProductId == product.Id && 
                                               (string.IsNullOrEmpty(productSummary.ProductVariantCombinationUid) 
                                                   ? string.IsNullOrEmpty(ubp.ProductVariantCombinationUid)
                                                   : ubp.ProductVariantCombinationUid == productSummary.ProductVariantCombinationUid), 
                                               cancellationToken);
                
                if (bagItem != null)
                {
                    _dbContext.UserBagProducts.Remove(bagItem);
                }

                ProductVariantCombination? selectedVariantCombination = null;
                if (!string.IsNullOrWhiteSpace(productSummary.ProductVariantCombinationUid))
                {
                    selectedVariantCombination = product.ProductVariantCombinations?
                        .FirstOrDefault(vc => vc.Uid == productSummary.ProductVariantCombinationUid);

                    if (selectedVariantCombination != null)
                    {
                        variantCombinationsToUpdate.Add((selectedVariantCombination, productSummary.BagQuantity));
                    }
                }

                // Capture product snapshot data
                var productName = product.Name ?? string.Empty;
                var productDescription = string.IsNullOrWhiteSpace(product.ProductDetail)
                    ? product.WhatIsIt ?? string.Empty
                    : $"{product.WhatIsIt ?? string.Empty} {product.ProductDetail}".Trim();

                // Get primary image URL (Priority == 0, or first active image)
                var primaryImageUrl = product.ProductMediaFiles?
                    .Where(pmf => pmf.MediaFile != null && pmf.MediaFile.IsActive)
                    .OrderBy(pmf => pmf.MediaFile.Priority)
                    .FirstOrDefault()?.MediaFile.Url;

                // Get variant types from selected combination or fallback to first active combination
                var variantTypes = new List<string>();
                var targetCombination = selectedVariantCombination ?? product.ProductVariantCombinations?.FirstOrDefault(vc => vc.IsActive);
                
                if (targetCombination?.CombinationOptions != null)
                {
                    variantTypes = targetCombination.CombinationOptions
                        .OrderBy(co => co.ProductVariantOption?.ProductVariant?.Id ?? 0)
                        .Select(co => co.ProductVariantOption?.Value)
                        .Where(v => !string.IsNullOrEmpty(v))
                        .ToList();
                }

                // Calculate countdown expiry date based on delivery time
                var deliveryDays = ParseDeliveryDays(productSummary.DeliveryTime);
                var countdownExpiryDate = DateTime.UtcNow.AddMinutes(deliveryDays * 24 * 60); // Convert days to minutes for accuracy

                orderProductAffiliates.Add(new OrderProductAffiliate
                {
                    Product = product,
                    ProductQuantity = productSummary.BagQuantity,
                    AffiliateId = null,
                    ProductVariantCombinationId = selectedVariantCombination?.Id,
                    ProductVariantCombinationUidSnapshot = selectedVariantCombination?.Uid,

                    OrderItemStatus = OrderStatusEnum.Processing,
                    CountdownExpiryDate = countdownExpiryDate,
                    IsRetryAllowed = true,

                    // Capture complete snapshot data
                    ProductNameSnapshot = productName,
                    ProductDescriptionSnapshot = productDescription,
                    ProductPriceSnapshot = (decimal)productSummary.Price, // Use ordered price
                    ProductMinPriceSnapshot = product.MinPrice,
                    ProductMaxPriceSnapshot = product.MaxPrice,
                    ProductBrandSnapshot = product.Brand,
                    PrimaryImageUrlSnapshot = primaryImageUrl,
                    CountryCodeSnapshot = product.Country?.Iso2,
                    CurrencyCodeSnapshot = product.Country?.Iso4,
                    ProductTypeSnapshot = (int)product.Type,
                    ProfileUidSnapshot = product.User?.Profile?.Uid,
                    ProfileUsernameSnapshot = product.User?.UserName,
                    ShippingCostSnapshot = productSummary.ShippingCost,
                    DeliveryTimeSnapshot = productSummary.DeliveryTime,
                    VariantTypesSnapshot = variantTypes.Any() ? Newtonsoft.Json.JsonConvert.SerializeObject(variantTypes) : null
                });
            }

            if (!orderProductAffiliates.Any())
            {
                return new SaveOrderResult { Success = false, Error = "No valid products found to create order" };
            }

            var order = new Order
            {
                Amount = request.Amount,
                Note = request.Note,
                StripePaymentMethodId = request.PaymentMethodId,
                Currency = currency,
                PaymentMethod = paymentMethod,
                Profile = profile,
                ShippingDetails = shippingDetails,
                BillingDetails = billingDetails,
                OrderProductAffiliates = orderProductAffiliates,
                OrderStatus = OrderStatusEnum.Pending, // Initial status is Pending
                RawRequest = paymentResponse.PaymentIntentId ?? string.Empty
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var (variantCombination, quantityToDecrease) in variantCombinationsToUpdate)
            {
                variantCombination.Quantity = Math.Max(0, variantCombination.Quantity - quantityToDecrease);

                if (variantCombination.Quantity == 0)
                {
                    variantCombination.IsAvailable = false;
                }
            }

            if (variantCombinationsToUpdate.Any())
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Set orderId in "P{sequentialId}" format (e.g., "P001") and save to database Uid
            var formattedOrderId = $"P{order.Id:D3}";
            order.Uid = formattedOrderId;
            checkoutSummary.OrderId = formattedOrderId;

            // Update individual product order IDs (P001-01, P001-02, etc.)
            int productIndex = 1;
            foreach (var opa in order.OrderProductAffiliates)
            {
                opa.Uid = $"{formattedOrderId}-{productIndex:D2}";
                productIndex++;
            }

            // Save the updated Uids back to database
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Update CheckoutSummary products with their ProductOrderUid
            productIndex = 1;
            foreach (var productSummary in checkoutSummary.Products)
            {
                productSummary.ProductOrderUid = $"{formattedOrderId}-{productIndex:D2}";
                productIndex++;
            }

            // Load all necessary navigation properties for email sending
            var orderWithDetails = await _dbContext.Orders
                .Include(o => o.Profile)
                    .ThenInclude(p => p.User)
                .Include(o => o.ShippingDetails)
                    .ThenInclude(sd => sd.CountryNavigation)
                .Include(o => o.Currency)
                .Include(o => o.PaymentMethod)
                .Include(o => o.OrderProductAffiliates)
                    .ThenInclude(opa => opa.Product)
                        .ThenInclude(p => p.User)
                .Include(o => o.OrderProductAffiliates)
                    .ThenInclude(opa => opa.Product)
                        .ThenInclude(p => p.ProductMediaFiles)
                            .ThenInclude(pmf => pmf.MediaFile)
                .FirstOrDefaultAsync(o => o.Id == order.Id, cancellationToken);

            // Send order confirmation emails to buyer and sellers
            if (orderWithDetails != null)
            {
                try
                {
                    await _emailService.SendOrderConfirmationEmailsAsync(orderWithDetails);
                    _logger.LogInformation("Order confirmation emails sent for order {OrderId}", formattedOrderId);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send order confirmation emails for order {OrderId}", formattedOrderId);
                    // Don't fail the order creation if email sending fails
                }
            }

            // Create wallet transactions for buyer and sellers
            WalletTransactionResponse? walletTransaction = null;
            if (orderWithDetails != null)
            {
                try
                {
                    walletTransaction = await CreateWalletTransactionsAsync(orderWithDetails, checkoutSummary, request, cancellationToken);
                    _logger.LogInformation("Wallet transactions created for order {OrderId}", formattedOrderId);
                }
                catch (Exception walletEx)
                {
                    _logger.LogError(walletEx, "Failed to create wallet transactions for order {OrderId}", formattedOrderId);
                    // Don't fail the order creation if wallet transaction creation fails
                }
            }

            return new SaveOrderResult { Success = true, WalletTransaction = walletTransaction };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving order to database: {Message}", ex.Message);
            return new SaveOrderResult { Success = false, Error = $"Exception: {ex.Message}" };
        }
    }

    private async Task<WalletTransactionResponse?> CreateWalletTransactionsAsync(
        Order order,
        CheckoutSummaryResponse checkoutSummary,
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (order == null || order.Profile == null) return null;

        var transactionsToAdd = new List<WalletTransaction>();
        WalletTransactionResponse? buyerTransactionResponse = null;

        // Get payment method details for card info
        string? cardType = null;
        string? cardLast4 = null;
        
        if (!string.IsNullOrWhiteSpace(request.PaymentMethodId))
        {
            try
            {
                var paymentMethod = await _stripeService.GetPaymentMethodAsync(request.PaymentMethodId);
                if (paymentMethod != null)
                {
                    cardType = paymentMethod.Brand; // e.g., "Visa", "Mastercard"
                    cardLast4 = paymentMethod.Last4;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get payment method details for wallet transaction");
            }
        }

        // 1. Create PURCHASE transaction for the buyer (negative amount - money out)
        var buyerTransaction = new WalletTransaction
        {
            ProfileId = order.ProfileId,
            TransactionType = TransactionTypeEnum.Purchase,
            Amount = -order.Amount, // Negative for money going out
            CurrencyId = order.CurrencyId,
            OrderId = order.Id,
            Description = order.Uid,
            CardNumberLast4 = cardLast4,
            CardType = cardType,
            TransactionDate = DateTime.UtcNow,
            Status = TransactionStatusEnum.Completed
        };
        transactionsToAdd.Add(buyerTransaction);

        // Map buyer transaction to response DTO for the payment API response
        buyerTransactionResponse = new WalletTransactionResponse
        {
            Uid = buyerTransaction.Uid,
            TransactionType = buyerTransaction.TransactionType.ToString(),
            Amount = buyerTransaction.Amount,
            CurrencyCode = order.Currency?.Code ?? "AED",
            Description = buyerTransaction.Description,
            TransactionDate = buyerTransaction.TransactionDate,
            Status = buyerTransaction.Status.ToString(),
            CardNumberLast4 = buyerTransaction.CardNumberLast4,
            CardType = buyerTransaction.CardType,
            // Seller names will be filled by the caller or we can do it here if needed.
            // For a purchase, we can populate seller names.
        };

        // 2. Create SALE transactions for each seller (positive amount - money in)
        // Group products by seller
        var productsBySeller = order.OrderProductAffiliates
            .Where(opa => opa.Product?.UserId != null)
            .GroupBy(opa => opa.Product.UserId);

        foreach (var sellerGroup in productsBySeller)
        {
            var sellerId = sellerGroup.Key;
            
            // Get seller's profile
            var sellerProfile = await _dbContext.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == sellerId, cancellationToken);

            if (sellerProfile == null) continue;

            // Calculate total amount for this seller's products
            decimal sellerAmount = 0;
            foreach (var opa in sellerGroup)
            {
                var productSummary = checkoutSummary.Products
                    .FirstOrDefault(p => p.Uid == opa.Product.Uid);
                
                if (productSummary != null)
                {
                    sellerAmount += productSummary.Price * opa.ProductQuantity;
                }
            }

            // Get seller's store name
            var sellerStore = await _dbContext.Stores
                .FirstOrDefaultAsync(s => s.UserId == sellerId && s.IsActive, cancellationToken);

            var sellerName = sellerStore?.Name ?? sellerProfile.User?.DisplayName ?? "Unknown Seller";

            var sellerTransaction = new WalletTransaction
            {
                ProfileId = sellerProfile.Id,
                TransactionType = TransactionTypeEnum.Sale,
                Amount = sellerAmount, // Positive for money coming in
                CurrencyId = order.CurrencyId,
                OrderId = order.Id,
                Description = order.Uid,
                SellerName = order.Profile.User?.DisplayName ?? "Customer", // Buyer's name from seller's perspective
                TransactionDate = DateTime.UtcNow,
                Status = TransactionStatusEnum.Completed
            };
            transactionsToAdd.Add(sellerTransaction);
        }

        // Save all transactions
        if (transactionsToAdd.Any())
        {
            _dbContext.WalletTransactions.AddRange(transactionsToAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Fill seller names for the buyer's response
        if (buyerTransactionResponse != null && order.OrderProductAffiliates != null)
        {
            var sellerIds = order.OrderProductAffiliates
                .Where(opa => opa.Product?.UserId != null)
                .Select(opa => opa.Product.UserId)
                .Distinct()
                .ToList();

            var sellerStores = await _dbContext.Stores
                .Where(s => sellerIds.Contains(s.UserId) && s.IsActive)
                .ToDictionaryAsync(s => s.UserId, s => s.Name, cancellationToken);

            buyerTransactionResponse.SellerNames = order.OrderProductAffiliates
                .Where(opa => opa.Product?.UserId != null)
                .Select(opa => 
                {
                    if (sellerStores.TryGetValue(opa.Product.UserId, out var storeName))
                        return storeName;
                    return opa.Product.User?.FirstName ?? "Unknown Seller";
                })
                .Distinct()
                .ToList();
        }

        return buyerTransactionResponse;
    }

    private static double ParseDeliveryDays(string deliveryTime)
    {
        if (string.IsNullOrWhiteSpace(deliveryTime)) return 7;
        var dt = deliveryTime.ToLower().Trim();
        var rangeMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)\s*-\s*(\d+)\s*(day|week)s?");
        if (rangeMatch.Success)
            return rangeMatch.Groups[3].Value == "week"
                ? int.Parse(rangeMatch.Groups[2].Value) * 7
                : int.Parse(rangeMatch.Groups[2].Value);
        var singleMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)\s*(day|week)s?");
        if (singleMatch.Success)
            return singleMatch.Groups[2].Value == "week"
                ? int.Parse(singleMatch.Groups[1].Value) * 7
                : int.Parse(singleMatch.Groups[1].Value);
        var minuteMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)\s*(minute|min)s?");
        if (minuteMatch.Success) return double.Parse(minuteMatch.Groups[1].Value) / 1440.0;
        var hourMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)\s*hours?");
        if (hourMatch.Success) return double.Parse(hourMatch.Groups[1].Value) / 24.0;
        var numMatch = System.Text.RegularExpressions.Regex.Match(dt, @"(\d+)");
        return numMatch.Success ? int.Parse(numMatch.Groups[1].Value) : 7;
    }
}
