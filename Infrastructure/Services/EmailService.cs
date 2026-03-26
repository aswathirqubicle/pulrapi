using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Extensions;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.Services;
using Core.Domain.Entities;
using System.Linq;
using Core.Application.Models.Email;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

namespace Core.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly AmazonSesEmailConfig _emailConfig;
        private readonly IApplicationDbContext _dbContext;
        private readonly IEmailLogoService _emailLogoService;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly Dictionary<string, string> _phoneCodeCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public EmailService(
            IConfiguration config, 
            ILogger<EmailService> logger, 
            IApplicationDbContext dbContext,
            IEmailLogoService emailLogoService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _emailLogoService = emailLogoService ?? throw new ArgumentNullException(nameof(emailLogoService));
            
            if (_emailConfig == null)
            {
                _emailConfig = new AmazonSesEmailConfig(config);
            }
        }

        public async Task SendMail(EmailParamsDto emailParams, bool includeAttachments = false)
        {
            try
            {
                // Substitute Apple Private Relay emails if CommunicationMail is available
                if (emailParams.To != null) emailParams.To = await ReplaceApplePrivateRelayEmailsAsync(emailParams.To);
                if (emailParams.Cc != null) emailParams.Cc = await ReplaceApplePrivateRelayEmailsAsync(emailParams.Cc);
                if (emailParams.Bcc != null) emailParams.Bcc = await ReplaceApplePrivateRelayEmailsAsync(emailParams.Bcc);

                // Validate email addresses before sending
                if (emailParams.To == null || !emailParams.To.Any())
                {
                    _logger.LogWarning("No recipient email addresses provided");
                    return;
                }

                // Filter out invalid email addresses and log warnings
                var validRecipients = new List<string>();
                foreach (var email in emailParams.To)
                {
                    if (IsValidEmailAddress(email))
                    {
                        validRecipients.Add(email);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid email address skipped: {Email}", email);
                    }
                }

                if (!validRecipients.Any())
                {
                    _logger.LogWarning("No valid recipient email addresses after validation");
                    return;
                }

                // Update email params with valid recipients
                emailParams.To = validRecipients;

                //emailParams.Bcc.Add("IF EVER NEEDED");
                if (emailParams.Attachments.Count > 0 && includeAttachments == true)
                {
                    await SendEmailWithAttachments(emailParams);
                    return;
                }

                await SendSimpleEmail(emailParams);

            }
            catch (AmazonSimpleEmailServiceException sesEx)
            {
                _logger.LogError(sesEx, "AWS SES error sending email. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, RequestId: {RequestId}", 
                    sesEx.StatusCode, sesEx.ErrorCode, sesEx.RequestId);
                
                // Log recipient emails for debugging (without sensitive data)
                _logger.LogError("Failed to send email to recipients: {Recipients}", 
                    string.Join(", ", emailParams.To?.Select(email => MaskEmail(email)) ?? new List<string>()));
                
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending email: {Message}. Recipients: {Recipients}", 
                    e.Message, string.Join(", ", emailParams.To?.Select(email => MaskEmail(email)) ?? new List<string>()));
                throw;
            }
        }

        private bool IsValidEmailAddress(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Use MailAddress to validate email format - it's more permissive than regex
                var mailAddress = new System.Net.Mail.MailAddress(email);
                
                // Additional check: ensure the address part is not empty
                if (string.IsNullOrWhiteSpace(mailAddress.Address))
                    return false;

                // Apple Private Relay addresses are valid
                // Format: xxxxx@privaterelay.appleid.com
                if (email.EndsWith("@privaterelay.appleid.com", StringComparison.OrdinalIgnoreCase))
                    return true;

                // Standard email validation
                return mailAddress.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "***";
            
            var parts = email.Split('@');
            if (parts.Length != 2)
                return "***";
            
            var localPart = parts[0];
            var domain = parts[1];
            
            if (localPart.Length <= 2)
                return $"**@{domain}";
            
            return $"{localPart.Substring(0, 2)}***@{domain}";
        }

        public async Task SendOrderConfirmationEmailsAsync(Order order)
        {
            try
            {
                if (order == null)
                {
                    _logger.LogWarning("Order is null, cannot send confirmation emails");
                    return;
                }

                // Get shipping fee from seller settings (default to 0 if not set)
                var sellerUserIds = order.OrderProductAffiliates?
                    .Where(opa => opa.Product?.UserId != null)
                    .Select(opa => opa.Product.UserId)
                    .Distinct()
                    .ToList() ?? new List<string>();

                decimal shippingFee = 0;
                if (sellerUserIds.Any())
                {
                    var sellerSettings = await _dbContext.SellerSettings
                        .Where(ss => sellerUserIds.Contains(ss.UserId))
                        .ToListAsync();
                    
                    // Sum up shipping costs from all sellers, defaulting to 0 if not set
                    shippingFee = sellerSettings.Sum(ss => ss.ShippingCosts ?? 0);
                }

                // Calculate product costs
                var productCost = order.OrderProductAffiliates?
                    .Sum(opa => (decimal)(opa.Product?.MinPrice ?? 0) * opa.ProductQuantity) ?? 0;

                // Calculate subtotal (products + shipping) without VAT
                var subtotal = productCost + shippingFee;

                // Calculate VAT (5% of subtotal)
                var vatRate = 0.05m;
                var estimatedVAT = subtotal * vatRate;

                // Calculate total (subtotal + VAT)
                var totalAmount = subtotal + estimatedVAT;

                // Prepare delivery address
                var deliveryAddress = $"{order.ShippingDetails?.Address}, {order.ShippingDetails?.City}, {order.ShippingDetails?.Region}, {order.ShippingDetails?.Country}";
                
                var orderDate = order.CreatedAt.ToString("dd MMMM yyyy");

                // Send email to buyer
                await SendBuyerOrderConfirmationEmailAsync(order, estimatedVAT, shippingFee, deliveryAddress, orderDate, totalAmount);

                // Send emails to all sellers
                await SendSellerOrderNotificationEmailsAsync(order, estimatedVAT, shippingFee, deliveryAddress, orderDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation emails for order {OrderId}", order?.Uid);
                // Don't throw - we don't want email failures to break the order process
            }
        }

        private async Task SendBuyerOrderConfirmationEmailAsync(Order order, decimal estimatedVAT, decimal shippingFee, string deliveryAddress, string orderDate, decimal totalAmount)
        {
            try
            {
                // Prefer FirstName for greeting; fall back to UserName, then shipping name, then display name.
                var buyerName = order.Profile?.User?.FirstName;
                if (string.IsNullOrWhiteSpace(buyerName))
                {
                    buyerName = order.Profile?.User?.UserName;
                }
                if (string.IsNullOrWhiteSpace(buyerName))
                {
                    buyerName = order.ShippingDetails?.FirstName?.Trim();
                }
                if (string.IsNullOrWhiteSpace(buyerName))
                {
                    buyerName = order.Profile?.User?.DisplayName ?? "Valued Customer";
                }

                // Priority logic for buyer email:
                // 1. SellerSettings.CommunicationMail (if available)
                // 2. User.Email (registered email)
                // Explicitly NOT using ShippingDetails.Email as per business requirements

                string buyerEmail = null;
                var buyerUserId = order.Profile?.UserId;

                if (!string.IsNullOrEmpty(buyerUserId))
                {
                    try 
                    {
                        var buyerSettings = await _dbContext.SellerSettings
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ss => ss.UserId == buyerUserId);
                        
                        if (!string.IsNullOrWhiteSpace(buyerSettings?.CommunicationMail))
                        {
                            buyerEmail = buyerSettings.CommunicationMail.Trim();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch seller settings for buyer {UserId} email resolution", buyerUserId);
                    }
                }

                if (string.IsNullOrWhiteSpace(buyerEmail))
                {
                    buyerEmail = order.Profile?.User?.Email?.Trim();
                }

                if (string.IsNullOrWhiteSpace(buyerEmail))
                {
                    _logger.LogWarning("No buyer email found for order {OrderId}", order.Uid);
                    return;
                }

                var orderSummaryUrl = $"{_config["ConsumerUrls:IosApp"]}/wallet/{order.Uid}";

                // Format phone number with country code
                var phoneNumber = order.ShippingDetails?.PhoneNumber ?? "";
                var countryCode = order.ShippingDetails?.CountryNavigation?.Iso2;
                var formattedPhoneNumber = await FormatPhoneNumberWithCountryCodeAsync(phoneNumber, countryCode);

                var buyerModel = new BuyerOrderConfirmationEmailModel
                {
                    RecipientName = buyerName,
                    RecipientEmail = buyerEmail,
                    OrderNumber = order.Uid,
                    OrderDate = orderDate,
                    TotalAmount = totalAmount,
                    EstimatedVAT = estimatedVAT,
                    ShippingFee = shippingFee,
                    Currency = order.Currency?.Code ?? "AED",
                    PaymentMethod = order.PaymentMethod?.Name ?? "Card",
                    DeliveryAddress = deliveryAddress,
                    PhoneNumber = formattedPhoneNumber,
                    OrderSummaryUrl = orderSummaryUrl,
                    Products = order.OrderProductAffiliates?.Select(opa => new OrderProductEmailModel
                    {
                        ProductName = opa.Product?.Name ?? "",
                        Brand = opa.Product?.Brand ?? "",
                        Quantity = opa.ProductQuantity,
                        Price = (decimal)(opa.Product?.MinPrice ?? 0),
                        ImageUrl = opa.Product?.ProductMediaFiles?.FirstOrDefault()?.MediaFile?.Url ?? "",
                        VariantDetails = ""
                    }).ToList() ?? new List<OrderProductEmailModel>()
                };

                var emailContent = EmailTemplateHelper.GenerateBuyerOrderConfirmationEmail(buyerModel);

                var emailParams = new EmailParamsDto
                {
                    To = new List<string> { buyerEmail },
                    From = _config["PulrEmails:Support"],
                    Subject = $"Order #{order.Uid} Confirmed – Thank You for Shopping with PULR",
                    Content = emailContent
                };

                // Add logo attachment using service (follows Dependency Inversion Principle)
                await emailParams.AddLogoAsync(_emailLogoService);

                await SendMail(emailParams, includeAttachments: emailParams.Attachments.Count > 0);
                _logger.LogInformation("Buyer confirmation email sent for order {OrderId} to {Email}", order.Uid, buyerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending buyer confirmation email for order {OrderId}", order.Uid);
            }
        }

        private async Task SendSellerOrderNotificationEmailsAsync(Order order, decimal estimatedVAT, decimal totalShippingFee, string deliveryAddress, string orderDate)
        {
            try
            {
                // Group products by seller
                var sellerGroups = order.OrderProductAffiliates?
                    .Where(opa => opa.Product?.User != null)
                    .GroupBy(opa => opa.Product.User)
                    .ToList();

                if (sellerGroups == null || !sellerGroups.Any())
                {
                    _logger.LogWarning("No sellers found for order {OrderId}", order.Uid);
                    return;
                }

                var ordersAreaUrl = $"{_config["ConsumerUrls:IosApp"]}/wallet/{order.Uid}";

                foreach (var sellerGroup in sellerGroups)
                {
                    var seller = sellerGroup.Key;
                    var sellerProducts = sellerGroup.ToList();

                    // Prefer FirstName for seller greeting; fall back to UserName, then display name.
                    var sellerName = seller.FirstName;
                    if (string.IsNullOrWhiteSpace(sellerName))
                    {
                        sellerName = seller.UserName;
                    }
                    if (string.IsNullOrWhiteSpace(sellerName))
                    {
                        sellerName = seller.DisplayName ?? seller.FirstName?.Trim();
                    }
                    if (string.IsNullOrWhiteSpace(sellerName))
                    {
                        sellerName = "Seller";
                    }

                    // Get seller settings
                    var sellerSettings = await _dbContext.SellerSettings
                        .FirstOrDefaultAsync(ss => ss.UserId == seller.Id);

                    // Priority 1: SellerSettings.CommunicationMail
                    // Priority 2: User.Email (registered email)
                    var sellerEmail = sellerSettings?.CommunicationMail;
                    
                    if (string.IsNullOrWhiteSpace(sellerEmail))
                    {
                         sellerEmail = seller.Email;
                    }

                    if (string.IsNullOrWhiteSpace(sellerEmail))
                    {
                        _logger.LogWarning("No email found for seller {SellerId} in order {OrderId}", seller.Id, order.Uid);
                        continue;
                    }

                    // Calculate seller's portion of the order
                    var sellerTotalAmount = sellerProducts.Sum(sp => (decimal)(sp.Product?.MinPrice ?? 0) * sp.ProductQuantity);

                    // Get seller's shipping cost (default to 0 if not set)
                    var sellerShippingFee = sellerSettings?.ShippingCosts ?? 0;

                    // Format phone number with country code
                    var phoneNumber = order.ShippingDetails?.PhoneNumber ?? "";
                    var countryCode = order.ShippingDetails?.CountryNavigation?.Iso2;
                    var formattedPhoneNumber = await FormatPhoneNumberWithCountryCodeAsync(phoneNumber, countryCode);

                    var sellerModel = new SellerOrderNotificationEmailModel
                    {
                        SellerName = sellerName,
                        SellerEmail = sellerEmail,
                        OrderNumber = order.Uid,
                        OrderDate = orderDate,
                        TotalAmount = sellerTotalAmount,
                        EstimatedVAT = sellerTotalAmount * 0.05m,
                        ShippingFee = sellerShippingFee,
                        Currency = order.Currency?.Code ?? "AED",
                        PaymentMethod = order.PaymentMethod?.Name ?? "Card",
                        DeliveryAddress = deliveryAddress,
                        PhoneNumber = formattedPhoneNumber,
                        OrdersAreaUrl = ordersAreaUrl,
                        Products = sellerProducts.Select(sp => new OrderProductEmailModel
                        {
                            ProductName = sp.Product?.Name ?? "",
                            Brand = sp.Product?.Brand ?? "",
                            Quantity = sp.ProductQuantity,
                            Price = (decimal)(sp.Product?.MinPrice ?? 0),
                            ImageUrl = sp.Product?.ProductMediaFiles?.FirstOrDefault()?.MediaFile?.Url ?? "",
                            VariantDetails = ""
                        }).ToList()
                    };

                    var emailContent = EmailTemplateHelper.GenerateSellerOrderNotificationEmail(sellerModel);

                    var emailParams = new EmailParamsDto
                    {
                        To = new List<string> { sellerEmail },
                        From = _config["PulrEmails:Support"],
                        Subject = $"New Order #{order.Uid} – Action Required",
                        Content = emailContent
                    };

                    // Add logo attachment using service (follows Dependency Inversion Principle)
                    await emailParams.AddLogoAsync(_emailLogoService);

                    await SendMail(emailParams, includeAttachments: emailParams.Attachments.Count > 0);
                    _logger.LogInformation("Seller notification email sent for order {OrderId} to {Email}", order.Uid, sellerEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending seller notification emails for order {OrderId}", order.Uid);
            }
        }

        public async Task SendOrderShippedEmailAsync(Order order, List<OrderProductAffiliate> shippedItems, string trackingNumber, string shippingProvider)
        {
            try
            {
                if (order == null)
                {
                    _logger.LogWarning("Order is null, cannot send shipped email");
                    return;
                }

                // Prefer FirstName for greeting; fall back to UserName, then shipping name, then display name.
                var buyerName = order.Profile?.User?.FirstName;
                if (string.IsNullOrWhiteSpace(buyerName))
                {
                    buyerName = order.Profile?.User?.UserName;
                }
                if (string.IsNullOrWhiteSpace(buyerName))
                {
                    buyerName = order.ShippingDetails?.FirstName?.Trim();
                }
                if (string.IsNullOrWhiteSpace(buyerName))
                {
                    buyerName = order.Profile?.User?.DisplayName ?? "Valued Customer";
                }

                // Priority logic for buyer email:
                // 1. SellerSettings.CommunicationMail (if user has seller settings)
                // 2. ShippingDetails.Email (entered during checkout)
                // 3. User.Email (registered email)
                
                string buyerEmail = null;
                
                // First, try to get CommunicationMail from SellerSettings
                var buyerUserId = order.Profile?.UserId;
                if (!string.IsNullOrWhiteSpace(buyerUserId))
                {
                    var buyerSettings = await _dbContext.SellerSettings
                        .FirstOrDefaultAsync(ss => ss.UserId == buyerUserId);
                    
                    if (buyerSettings != null && !string.IsNullOrWhiteSpace(buyerSettings.CommunicationMail))
                    {
                        buyerEmail = buyerSettings.CommunicationMail.Trim();
                        _logger.LogInformation("Using CommunicationMail for order {OrderId}", order.Uid);
                    }
                }
                
                // If CommunicationMail is not available, fall back to shipping/user email
                if (string.IsNullOrWhiteSpace(buyerEmail))
                {
                    buyerEmail = (order.ShippingDetails?.Email ?? order.Profile?.User?.Email)?.Trim();
                }

                if (string.IsNullOrWhiteSpace(buyerEmail))
                {
                    _logger.LogWarning("No buyer email found for order {OrderId}", order.Uid);
                    return;
                }

                var orderSummaryUrl = $"{_config["ConsumerUrls:IosApp"]}/wallet/{order.Uid}";

                // Prepare delivery address
                var deliveryAddress = $"{order.ShippingDetails?.Address}, {order.ShippingDetails?.City}, {order.ShippingDetails?.Region}, {order.ShippingDetails?.Country}";
                
                var orderDate = order.CreatedAt.ToString("dd MMMM yyyy");
                var shippedOn = DateTime.UtcNow.ToString("dd MMMM yyyy");

                // Format phone number with country code
                var phoneNumber = order.ShippingDetails?.PhoneNumber ?? "";
                var countryCode = order.ShippingDetails?.CountryNavigation?.Iso2;
                var formattedPhoneNumber = await FormatPhoneNumberWithCountryCodeAsync(phoneNumber, countryCode);

                // Map shipped items to email product models
                var shippedProducts = shippedItems?.Select(opa => new OrderProductEmailModel
                {
                    ProductName = opa.Product?.Name ?? opa.ProductNameSnapshot ?? "",
                    Brand = opa.Product?.Brand ?? opa.ProductBrandSnapshot ?? "",
                    Quantity = opa.ProductQuantity,
                    Price = (decimal)(opa.Product?.MinPrice ?? opa.ProductMinPriceSnapshot ?? 0),
                    ImageUrl = opa.Product?.ProductMediaFiles?.FirstOrDefault()?.MediaFile?.Url ?? opa.PrimaryImageUrlSnapshot ?? "",
                    VariantDetails = ""
                }).ToList() ?? new List<OrderProductEmailModel>();

                var emailModel = new BuyerOrderShippedEmailModel
                {
                    RecipientName = buyerName,
                    RecipientEmail = buyerEmail,
                    OrderNumber = order.Uid,
                    OrderDate = orderDate,
                    ShippedOn = shippedOn,
                    TrackingNumber = trackingNumber,
                    DeliveryService = shippingProvider,
                    DeliveryAddress = deliveryAddress,
                    PhoneNumber = formattedPhoneNumber,
                    OrderSummaryUrl = orderSummaryUrl,
                    Products = shippedProducts
                };

                var emailContent = EmailTemplateHelper.GenerateBuyerOrderShippedEmail(emailModel);

                var emailParams = new EmailParamsDto
                {
                    To = new List<string> { buyerEmail },
                    From = _config["PulrEmails:Support"],
                    Subject = $"Your order is on its way! – Order #{order.Uid}",
                    Content = emailContent
                };

                // Add logo attachment using service (follows Dependency Inversion Principle)
                await emailParams.AddLogoAsync(_emailLogoService);

                await SendMail(emailParams, includeAttachments: emailParams.Attachments.Count > 0);
                _logger.LogInformation("Order shipped email sent for order {OrderId} to {Email} with {ProductCount} products",
                    order.Uid, buyerEmail, shippedProducts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order shipped email for order {OrderId}", order?.Uid);
                // Don't throw - we don't want email failures to break the order process
            }
        }

        public async Task SendOrderCountdownExpiredEmailAsync(Order order, OrderProductAffiliate orderItem)
        {
            try
            {
                if (order == null || orderItem == null)
                {
                    _logger.LogWarning("Order or order item is null, cannot send countdown expired email");
                    return;
                }

                var buyerName = order.Profile?.User?.FirstName ?? order.Profile?.User?.UserName ?? "Valued Customer";
                var buyerEmail = order.Profile?.User?.Email;

                if (string.IsNullOrWhiteSpace(buyerEmail))
                {
                    _logger.LogWarning("No buyer email found for order {OrderId}", order.Uid);
                    return;
                }

                var orderSummaryUrl = $"{_config["ConsumerUrls:IosApp"]}/wallet/{order.Uid}";

                var emailContent = $@"
                    <h1>Order Action Required</h1>
                    <p>Dear {buyerName},</p>
                    <p>The delivery countdown for your order #{order.Uid} has expired. The seller failed to ship your item within the expected time.</p>
                    <p>You can now choose to:</p>
                    <ul>
                        <li><strong>Refund:</strong> Get a full refund for this item</li>
                        <li><strong>Reorder:</strong> Give the seller another chance to ship (one-time only)</li>
                    </ul>
                    <p><a href='{orderSummaryUrl}'>View Order Details</a></p>
                    <p>Please take action within 7 days.</p>
                    <p>Best regards,<br/>Pulr Team</p>
                ";

                var emailParams = new EmailParamsDto
                {
                    To = new List<string> { buyerEmail },
                    From = _config["PulrEmails:Support"],
                    Subject = $"Action Required: Order #{order.Uid} - Countdown Expired",
                    Content = emailContent
                };

                await emailParams.AddLogoAsync(_emailLogoService);
                await SendMail(emailParams);

                _logger.LogInformation("Countdown expired email sent for order {OrderId} to {Email}", order.Uid, buyerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending countdown expired email for order {OrderId}", order?.Uid);
            }
        }

        public async Task SendOrderRefundedEmailAsync(Order order, OrderProductAffiliate orderItem, decimal refundAmount)
        {
            try
            {
                if (order == null || orderItem == null)
                {
                    _logger.LogWarning("Order or order item is null, cannot send refunded email");
                    return;
                }

                var buyerName = order.Profile?.User?.FirstName ?? order.Profile?.User?.UserName ?? "Valued Customer";
                var buyerEmail = order.Profile?.User?.Email;

                if (string.IsNullOrWhiteSpace(buyerEmail))
                {
                    _logger.LogWarning("No buyer email found for order {OrderId}", order.Uid);
                    return;
                }

                var emailContent = $@"
                    <h1>Refund Processed</h1>
                    <p>Dear {buyerName},</p>
                    <p>Your refund for order #{order.Uid} has been processed successfully.</p>
                    <p><strong>Refund Amount:</strong> {refundAmount:C}</p>
                    <p>The amount has been credited to your Pulr wallet.</p>
                    <p>If you have any questions, please contact our support team.</p>
                    <p>Best regards,<br/>Pulr Team</p>
                ";

                var emailParams = new EmailParamsDto
                {
                    To = new List<string> { buyerEmail },
                    From = _config["PulrEmails:Support"],
                    Subject = $"Refund Processed - Order #{order.Uid}",
                    Content = emailContent
                };

                await emailParams.AddLogoAsync(_emailLogoService);
                await SendMail(emailParams);

                _logger.LogInformation("Refunded email sent for order {OrderId} to {Email}", order.Uid, buyerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refunded email for order {OrderId}", order?.Uid);
            }
        }

        public async Task SendOrderReorderedEmailAsync(Order order, OrderProductAffiliate orderItem)
        {
            try
            {
                if (order == null || orderItem == null)
                {
                    _logger.LogWarning("Order or order item is null, cannot send reordered email");
                    return;
                }

                // Notify seller about the reorder
                var sellerEmail = orderItem.Product?.User?.Email;
                if (string.IsNullOrWhiteSpace(sellerEmail))
                {
                    _logger.LogWarning("No seller email found for order {OrderId}", order.Uid);
                    return;
                }

                var sellerName = orderItem.Product?.User?.UserName ?? "Seller";
                var productName = orderItem.Product?.Name ?? orderItem.ProductNameSnapshot ?? "Product";

                var emailContent = $@"
                    <h1>Order Reordered</h1>
                    <p>Dear {sellerName},</p>
                    <p>A buyer has chosen to reorder your product after the original countdown expired.</p>
                    <p><strong>Product:</strong> {productName}</p>
                    <p><strong>Order #:</strong> {order.Uid}</p>
                    <p>Please ship this item within the new countdown period to avoid further issues.</p>
                    <p>Best regards,<br/>Pulr Team</p>
                ";

                var emailParams = new EmailParamsDto
                {
                    To = new List<string> { sellerEmail },
                    From = _config["PulrEmails:Support"],
                    Subject = $"Order Reordered - {productName}",
                    Content = emailContent
                };

                await emailParams.AddLogoAsync(_emailLogoService);
                await SendMail(emailParams);

                _logger.LogInformation("Reordered email sent for order {OrderId} to seller {Email}", order.Uid, sellerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reordered email for order {OrderId}", order?.Uid);
            }
        }

        private async Task SendSimpleEmail(EmailParamsDto emailParams)
        {
            using var sender = new AmazonSimpleEmailServiceClient(_emailConfig,
                                                                  RegionEndpoint.MESouth1);
            string from = String.IsNullOrWhiteSpace(emailParams.From) ? _config["PulrEmails:Support"] : emailParams.From;
            
            // Filter and validate email addresses
            var validToAddresses = emailParams.To?.Where(IsValidEmailAddress).ToList() ?? new List<string>();
            var validCcAddresses = emailParams.Cc?.Where(IsValidEmailAddress).ToList() ?? new List<string>();
            var validBccAddresses = emailParams.Bcc?.Where(IsValidEmailAddress).ToList() ?? new List<string>();

            if (!validToAddresses.Any())
            {
                _logger.LogWarning("No valid 'To' email addresses after validation");
                return;
            }

            var destination = new Destination()
            {
                BccAddresses = validBccAddresses,
                CcAddresses = validCcAddresses,
                ToAddresses = validToAddresses
            };
            
            var body = new Body
            {
                Html = new Content(emailParams.Content),
                Text = new Content("If you can't see this email, please use a mail client that supports HTML.") // fallback

            };
            var message = new Message()
            {
                // Body = new Body(new Content(emailParams.Content)),
                Body = body,
                Subject = new Content(emailParams.Subject)
            };
            
            var emailRequest = new SendEmailRequest(from, destination, message);
            
            try
            {
                var response = await sender.SendEmailAsync(emailRequest, new CancellationToken());
                _logger.LogInformation("Email sent successfully. MessageId: {MessageId}, To: {Recipients}", 
                    response.MessageId, string.Join(", ", validToAddresses.Select(MaskEmail)));
            }
            catch (AmazonSimpleEmailServiceException sesEx)
            {
                // Log detailed SES error information
                _logger.LogError(sesEx, 
                    "AWS SES error sending email. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, ErrorType: {ErrorType}, RequestId: {RequestId}, Message: {Message}",
                    sesEx.StatusCode, sesEx.ErrorCode, sesEx.ErrorType, sesEx.RequestId, sesEx.Message);
                
                // Check if it's a specific error that we can handle
                if (sesEx.ErrorCode == "MessageRejected")
                {
                    _logger.LogError("Email was rejected by AWS SES. This might be due to: invalid email addresses, spam content, or domain authentication issues.");
                }
                else if (sesEx.ErrorCode == "MailFromDomainNotVerifiedException")
                {
                    _logger.LogError("The 'From' domain is not verified in AWS SES. Please verify the domain: {FromDomain}", from);
                }
                else if (sesEx.ErrorCode == "ConfigurationSetDoesNotExistException")
                {
                    _logger.LogError("The configuration set does not exist in AWS SES.");
                }
                
                // Re-throw to allow caller to handle
                throw;
            }
        }

        private async Task SendEmailWithAttachments(EmailParamsDto emailParams)
        {
            try
            {
                using (var client = new AmazonSimpleEmailServiceClient(_emailConfig, RegionEndpoint.MESouth1))
                {
                    // Validate email addresses before sending
                    var validToAddresses = emailParams.To?.Where(IsValidEmailAddress).ToList() ?? new List<string>();
                    var validBccAddresses = emailParams.Bcc?.Where(IsValidEmailAddress).ToList() ?? new List<string>();

                    if (!validToAddresses.Any())
                    {
                        _logger.LogWarning("No valid 'To' email addresses for email with attachments");
                        return;
                    }

                    var bodyBuilder = new BodyBuilder();

                    bodyBuilder.HtmlBody = emailParams.Content;
                    
                    // Set a clean TextBody by stripping HTML or using a fallback message
                    // Using a simple fallback to avoid sending raw HTML in the text part
                    bodyBuilder.TextBody = "Please use an HTML-capable email client to view this message.";

                    foreach (var attachment in emailParams.Attachments)
                    {
                        // Ensure the stream is at the beginning before reading
                        if (attachment.ContentStream.CanSeek)
                        {
                            attachment.ContentStream.Seek(0, SeekOrigin.Begin);
                        }
                        var byteArray = FileHelper.streamToByteArray(attachment.ContentStream);
                        
                        if (byteArray == null || byteArray.Length == 0)
                        {
                            _logger.LogWarning("Skipping attachment {AttachmentName} because it has no content.", attachment.Name);
                            continue;
                        }
                        
                        // Parse MIME type if provided, otherwise let MimeKit infer it
                        MimeKit.ContentType contentType = null;
                        if (!string.IsNullOrEmpty(attachment.MimeType))
                        {
                            contentType = MimeKit.ContentType.Parse(attachment.MimeType);
                        }
                        
                        if (!string.IsNullOrEmpty(attachment.ContentId))
                        {
                            MimePart mimePart; // Use MimePart specifically for images
                            if (contentType != null)
                            {
                                mimePart = bodyBuilder.LinkedResources.Add(attachment.Name, byteArray, contentType) as MimePart;
                            }
                            else
                            {
                                mimePart = bodyBuilder.LinkedResources.Add(attachment.Name, byteArray) as MimePart;
                            }

                            if (mimePart != null)
                            {
                                // Set ContentId - MimeKit will wrap it in <> if needed during serialization
                                mimePart.ContentId = attachment.ContentId;
                                mimePart.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                                mimePart.ContentTransferEncoding = ContentEncoding.Base64;
                                mimePart.FileName = attachment.Name;
                            }
                        }
                        else
                        {
                            if (contentType != null)
                            {
                                bodyBuilder.Attachments.Add(attachment.Name, byteArray, contentType);
                            }
                            else
                            {
                                bodyBuilder.Attachments.Add(attachment.Name, byteArray);
                            }
                        }
                    }

                    var mimeMessage = new MimeMessage();
                    mimeMessage.From.Add(new MailboxAddress("", emailParams.From));
                    mimeMessage.To.AddRange(validToAddresses.ConvertAll(email => new MailboxAddress("", email)));
                    if (validBccAddresses.Any())
                    {
                        mimeMessage.Bcc.AddRange(validBccAddresses.ConvertAll(email => new MailboxAddress("", email)));
                    }

                    mimeMessage.Subject = emailParams.Subject;
                    mimeMessage.Body = bodyBuilder.ToMessageBody();
                    
                    using (var messageStream = new MemoryStream())
                    {
                        await mimeMessage.WriteToAsync(messageStream);
                        messageStream.Position = 0; // CRITICAL: Reset position before sending to AWS SES
                        
                        var sendRequest = new SendRawEmailRequest { RawMessage = new RawMessage(messageStream) };
                        var response = await client.SendRawEmailAsync(sendRequest);
                        _logger.LogInformation("Email with attachments sent successfully via SES Raw. MessageId: {MessageId}, To: {Recipients}", 
                            response.MessageId, string.Join(", ", validToAddresses.Select(MaskEmail)));
                    }
                }
            }
            catch (AmazonSimpleEmailServiceException sesEx)
            {
                _logger.LogError(sesEx, 
                    "AWS SES error sending email with attachments. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}, ErrorType: {ErrorType}, RequestId: {RequestId}, Message: {Message}",
                    sesEx.StatusCode, sesEx.ErrorCode, sesEx.ErrorType, sesEx.RequestId, sesEx.Message);
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending email with attachments: {Message}. Recipients: {Recipients}", 
                    e.Message, string.Join(", ", emailParams.To?.Select(email => MaskEmail(email)) ?? new List<string>()));
                throw;
            }
        }

        private async Task<string> FormatPhoneNumberWithCountryCodeAsync(string phoneNumber, string countryCode)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return "";
            }

            var trimmedPhone = phoneNumber.Trim();

            // If phone number already starts with +, it already has country code
            if (trimmedPhone.StartsWith("+"))
            {
                return trimmedPhone;
            }

            // If we have a country code (ISO2), try to get the phone country code from API
            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                var phoneCountryCode = await GetPhoneCountryCodeAsync(countryCode);
                if (!string.IsNullOrWhiteSpace(phoneCountryCode))
                {
                    // Remove any leading zeros or spaces
                    var cleanPhone = trimmedPhone.TrimStart('0').Trim();
                    return $"+{phoneCountryCode} {cleanPhone}";
                }
            }

            // If no country code mapping found, return phone as-is
            return trimmedPhone;
        }

        private async Task<string> GetPhoneCountryCodeAsync(string iso2Code)
        {
            if (string.IsNullOrWhiteSpace(iso2Code))
            {
                return null;
            }

            // Check cache first
            if (_phoneCodeCache.TryGetValue(iso2Code, out var cachedCode))
            {
                return cachedCode;
            }

            try
            {
                // Use REST Countries API (free, no API key required)
                // Alternative: You can also add PhoneCode field to Country entity and seed it once
                var apiUrl = $"https://restcountries.com/v3.1/alpha/{iso2Code}?fields=idd";
                
                var response = await _httpClient.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonContent);
                    
                    if (doc.RootElement.TryGetProperty("idd", out var iddElement))
                    {
                        if (iddElement.TryGetProperty("root", out var rootElement))
                        {
                            var root = rootElement.GetString();
                            if (!string.IsNullOrWhiteSpace(root))
                            {
                                // Some countries have suffixes, get the first one if it's an array
                                string suffix = "";
                                if (iddElement.TryGetProperty("suffixes", out var suffixesElement) && suffixesElement.ValueKind == JsonValueKind.Array && suffixesElement.GetArrayLength() > 0)
                                {
                                    suffix = suffixesElement[0].GetString();
                                }
                                
                                var phoneCode = root + suffix;
                                
                                // Cache the result
                                _phoneCodeCache[iso2Code] = phoneCode;
                                return phoneCode;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch phone country code for {Iso2Code} from API", iso2Code);
            }

            // Fallback: Return null if API call fails
            return null;
        }

        private async Task<List<string>> ReplaceApplePrivateRelayEmailsAsync(List<string> emails)
        {
            if (emails == null || !emails.Any())
                return emails;

            var result = new List<string>();
            bool modified = false;

            foreach (var email in emails)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    result.Add(email);
                    continue;
                }

                var trimmedEmail = email.Trim();
                if (trimmedEmail.EndsWith("@privaterelay.appleid.com", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Attempting to substitute Apple Private Relay email: {Email}", MaskEmail(trimmedEmail));
                    try
                    {
                        var normalizedEmail = trimmedEmail.ToUpperInvariant();
                        var user = await _dbContext.Users
                            .AsNoTracking()
                            .Where(u => u.NormalizedEmail == normalizedEmail || u.Email.ToLower() == trimmedEmail.ToLower())
                            .Select(u => new { u.Id })
                            .FirstOrDefaultAsync();

                        if (user != null)
                        {
                            var sellerSettings = await _dbContext.SellerSettings
                                .AsNoTracking()
                                .Where(ss => ss.UserId == user.Id)
                                .Select(ss => ss.CommunicationMail)
                                .FirstOrDefaultAsync();

                            if (!string.IsNullOrWhiteSpace(sellerSettings))
                            {
                                 _logger.LogInformation("Substituted Apple Private Relay email {Email} with {CommMail}", 
                                    MaskEmail(trimmedEmail), MaskEmail(sellerSettings));
                                result.Add(sellerSettings.Trim());
                                modified = true;
                                continue;
                            }
                        }
                        
                         _logger.LogWarning("No CommunicationMail found for relay email {Email}. User found: {UserFound}", 
                            MaskEmail(trimmedEmail), user != null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error substituting Apple private relay email {Email}", trimmedEmail);
                        // Fallback to original
                    }
                }
                result.Add(trimmedEmail);
            }

            return modified ? result : emails;
        }

    }
}
