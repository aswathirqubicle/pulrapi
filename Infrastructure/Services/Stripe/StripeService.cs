using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models.Stripe;
using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Core.Infrastructure.Services.Stripe;

public class StripeService : IStripeService
{
    private readonly StripeClient _stripeClient;
    private readonly ILogger<StripeService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly string _webhookSecret;
    private readonly string _publishableKey;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger, IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        
        var secretKey = _configuration["Stripe:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("Stripe:SecretKey is not configured in appsettings.json");
        }
        
        var httpClient = new SystemNetHttpClient(
            new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(30) });
        _stripeClient = new StripeClient(secretKey, httpClient: httpClient);
        _publishableKey = _configuration["Stripe:PublishableKey"] ?? string.Empty;
        _webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;
    }

    public async Task<CreatePaymentResponse> CreatePaymentAsync(CreatePaymentRequest request)
    {
        try
        {
            // Always work with the logged-in user's customer (creates if needed)
            var user = await GetCurrentUserAsync();
            var customer = await GetOrCreateCustomerForUserAsync(user);

            var paymentIntentService = new PaymentIntentService(_stripeClient);
            var amountInSmallestUnit = (long)Math.Round(request.Amount * 100m, MidpointRounding.AwayFromZero);

            PaymentIntent paymentIntent;
            string customerSessionClientSecret = string.Empty;

            var metadata = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(request.OrderId))
            {
                metadata.Add("OrderId", request.OrderId);
            }

            if (!string.IsNullOrWhiteSpace(request.PaymentMethodId))
            {
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = amountInSmallestUnit,
                    Currency = request.Currency,
                    Customer = customer.Id,
                    PaymentMethod = request.PaymentMethodId,
                    Confirm = true,
                    OffSession = false,
                    ReturnUrl = request.ReturnUrl ?? "https://app.pulr.co/payment-complete",
                    Metadata = metadata
                };

                paymentIntent = await CreatePaymentIntentWithRetryAsync(paymentIntentService, paymentIntentOptions);
            }
            else
            {
                var customerSession = await CreateCustomerSessionAsync(customer.Id);

                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = amountInSmallestUnit,
                    Currency = request.Currency,
                    Customer = customer.Id,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    },
                    Metadata = metadata
                };

                paymentIntent = await CreatePaymentIntentWithRetryAsync(paymentIntentService, paymentIntentOptions);
                customerSessionClientSecret = customerSession.ClientSecret;
            }

            return new CreatePaymentResponse
            {
                PaymentIntent = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                CustomerSessionClientSecret = customerSessionClientSecret,
                Customer = customer.Id,
                PublishableKey = _publishableKey,
                RequiresAction = paymentIntent.Status == "requires_action",
                Status = paymentIntent.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error creating payment: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<CreateCustomerSessionResponse> CreateCustomerSessionAsync(CreateCustomerSessionRequest request)
    {
        try
        {
            User? user = null;
            string? userId = null;

            // Endpoint requires [Authorize] — user is always authenticated here.
            // Always derive customer identity from the authenticated user; never trust caller-supplied CustomerId.
            userId = _currentUserService.GetUserId();
            user = await _currentUserService.GetUserAsync(skipDetails: true);

            if (user != null)
            {
                request.Email = user.Email;
                request.Name = user.FirstName?.Trim();
                request.CustomerId = user.StripeCustomerId; // override any caller-supplied value
            }

            Customer customer;
            var customerService = new CustomerService(_stripeClient);

            if (!string.IsNullOrWhiteSpace(request.CustomerId))
            {
                customer = await customerService.GetAsync(request.CustomerId);
            }
            else
            {
                customer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = request.Email,
                    Name = request.Name
                });

                if (user != null && string.IsNullOrWhiteSpace(user.StripeCustomerId))
                {
                    await SaveCustomerIdToUserAsync(userId!, customer.Id);
                }
            }

            // Create SetupIntent for payment element
            var setupIntentService = new SetupIntentService(_stripeClient);
            var setupIntent = await setupIntentService.CreateAsync(new SetupIntentCreateOptions
            {
                Customer = customer.Id,
                PaymentMethodTypes = new List<string> { "card" }
            });

            // Create customer session with payment_element enabled
            var customerSession = await CreateCustomerSessionWithPaymentElementAsync(customer.Id);

            return new CreateCustomerSessionResponse
            {
                ClientSecret = setupIntent.ClientSecret,
                CustomerId = customer.Id,
                CustomerSessionSecret = customerSession.ClientSecret,
                PublishableKey = _publishableKey
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error creating customer session: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer session: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<CustomerResponse> GetCustomerAsync()
    {
        try
        {
            var user = await GetCurrentUserWithStripeCustomerAsync();
            var customerService = new CustomerService(_stripeClient);
            var customer = await customerService.GetAsync(user.StripeCustomerId!);

            return new CustomerResponse
            {
                Id = customer.Id,
                Email = customer.Email ?? string.Empty,
                Name = customer.Name ?? string.Empty,
                Phone = customer.Phone ?? string.Empty,
                Created = customer.Created,
                Description = customer.Description ?? string.Empty
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error retrieving customer: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<IReadOnlyList<PaymentMethodResponse>> GetSavedPaymentMethodsAsync()
    {
        try
        {
            var user = await GetCurrentUserWithStripeCustomerAsync();
            var customerId = user.StripeCustomerId!;

            var customerService = new CustomerService(_stripeClient);
            var paymentMethodService = new PaymentMethodService(_stripeClient);

            var customer = await customerService.GetAsync(customerId);
            var defaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId;

            var listOptions = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card"
            };

            var paymentMethods = await paymentMethodService.ListAsync(listOptions);

            return paymentMethods
                .Select(pm => new PaymentMethodResponse
                {
                    Id = pm.Id,
                    Brand = pm.Card?.Brand,
                    Last4 = pm.Card?.Last4,
                    ExpMonth = (int)(pm.Card?.ExpMonth ?? 0),
                    ExpYear = (int)(pm.Card?.ExpYear ?? 0),
                    IsDefault = pm.Id == defaultPaymentMethodId
                })
                .ToList();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error retrieving saved payment methods: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving saved payment methods: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<PaymentMethodResponse?> GetPaymentMethodAsync(string paymentMethodId)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodId))
        {
            return null;
        }

        try
        {
            var user = await GetCurrentUserWithStripeCustomerAsync();
            var customerId = user.StripeCustomerId!;

            var paymentMethodService = new PaymentMethodService(_stripeClient);
            var paymentMethod = await paymentMethodService.GetAsync(paymentMethodId);

            if (paymentMethod.CustomerId != customerId)
            {
                // Do not leak existence of another customer's payment method
                throw new UnauthorizedAccessException("Payment method does not belong to the current user.");
            }

            var customerService = new CustomerService(_stripeClient);
            var customer = await customerService.GetAsync(customerId);
            var defaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId;

            return new PaymentMethodResponse
            {
                Id = paymentMethod.Id,
                Brand = paymentMethod.Card?.Brand ?? string.Empty,
                Last4 = paymentMethod.Card?.Last4 ?? string.Empty,
                ExpMonth = (int)(paymentMethod.Card?.ExpMonth ?? 0),
                ExpYear = (int)(paymentMethod.Card?.ExpYear ?? 0),
                IsDefault = paymentMethod.Id == defaultPaymentMethodId
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error retrieving payment method: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<SaveCardResponse> SaveCardAsync(SaveCardRequest request)
    {
        try
        {
            if (!ValidateCardDetails(request, out var validationError))
            {
                return new SaveCardResponse { Success = false, Error = validationError };
            }

            var user = await GetCurrentUserAsync();
            var customer = await GetOrCreateCustomerForUserAsync(user);

            var paymentMethodService = new PaymentMethodService(_stripeClient);
            
            // Get last 4 digits of card number for comparison
            var cardLast4 = request.CardNumber.Length >= 4 
                ? request.CardNumber.Substring(request.CardNumber.Length - 4) 
                : request.CardNumber;

            // Check if user already has this card
            var existingPaymentMethods = await paymentMethodService.ListAsync(new PaymentMethodListOptions
            {
                Customer = customer.Id,
                Type = "card"
            });

            global::Stripe.PaymentMethod existingCard = null;
            foreach (var pm in existingPaymentMethods.Data)
            {
                if (pm.Card != null && 
                    pm.Card.Last4 == cardLast4 &&
                    pm.Card.ExpMonth == request.ExpMonth &&
                    pm.Card.ExpYear == request.ExpYear)
                {
                    existingCard = pm;
                    break;
                }
            }

            var customerService = new CustomerService(_stripeClient);
            var customerDetails = await customerService.GetAsync(customer.Id);
            bool isDefault = false;
            string? message = null;

            if (existingCard != null)
            {
                // Card already exists
                isDefault = customerDetails.InvoiceSettings?.DefaultPaymentMethodId == existingCard.Id;

                // Only update if SetAsDefault is true and card is not currently default
                if (request.SetAsDefault && !isDefault)
                {
                    await customerService.UpdateAsync(customer.Id, new CustomerUpdateOptions
                    {
                        InvoiceSettings = new CustomerInvoiceSettingsOptions
                        {
                            DefaultPaymentMethod = existingCard.Id
                        }
                    });
                    isDefault = true;
                    message = "Card already exists. Default status updated.";
                }
                else
                {
                    message = "Card is already added.";
                }

                return new SaveCardResponse
                {
                    Success = true,
                    PaymentMethodId = existingCard.Id,
                    CustomerId = customer.Id,
                    IsDefault = isDefault,
                    Message = message
                };
            }

            // Card doesn't exist - create new one
            var paymentMethod = await paymentMethodService.CreateAsync(new PaymentMethodCreateOptions
            {
                Type = "card",
                Card = new PaymentMethodCardOptions
                {
                    Number = request.CardNumber,
                    ExpMonth = request.ExpMonth,
                    ExpYear = request.ExpYear,
                    Cvc = request.Cvc
                }
            });

            await paymentMethodService.AttachAsync(paymentMethod.Id, new PaymentMethodAttachOptions
            {
                Customer = customer.Id
            });

            // Set as default if requested
            if (request.SetAsDefault)
            {
                await customerService.UpdateAsync(customer.Id, new CustomerUpdateOptions
                {
                    InvoiceSettings = new CustomerInvoiceSettingsOptions
                    {
                        DefaultPaymentMethod = paymentMethod.Id
                    }
                });
                isDefault = true;
            }

            return new SaveCardResponse
            {
                Success = true,
                PaymentMethodId = paymentMethod.Id,
                CustomerId = customer.Id,
                IsDefault = isDefault,
                RequiresAction = false,
                Status = "succeeded"
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error saving card: {Message}", ex.Message);
            return new SaveCardResponse { Success = false, Error = "There was a problem saving your card. Please check your card details and try again." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving card: {Message}", ex.Message);
            return new SaveCardResponse { Success = false, Error = "An unexpected error occurred while saving your card." };
        }
    }

    public async Task<bool> DetachPaymentMethodAsync(string paymentMethodId)
    {
        try
        {
            var user = await GetCurrentUserWithStripeCustomerAsync();
            var paymentMethodService = new PaymentMethodService(_stripeClient);
            var paymentMethod = await paymentMethodService.GetAsync(paymentMethodId);

            if (paymentMethod.CustomerId != user.StripeCustomerId)
            {
                throw new UnauthorizedAccessException("Payment method does not belong to the current user.");
            }

            await paymentMethodService.DetachAsync(paymentMethodId);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error detaching payment method: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detaching payment method: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<bool> SetDefaultPaymentMethodAsync(string paymentMethodId)
    {
        try
        {
            var user = await GetCurrentUserWithStripeCustomerAsync();
            var paymentMethodService = new PaymentMethodService(_stripeClient);
            var paymentMethod = await paymentMethodService.GetAsync(paymentMethodId);

            if (paymentMethod.CustomerId != user.StripeCustomerId)
            {
                throw new UnauthorizedAccessException("Payment method does not belong to the current user.");
            }

            var customerService = new CustomerService(_stripeClient);
            await customerService.UpdateAsync(user.StripeCustomerId, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            });

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error setting default payment method: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<SetupIntentResponse> CreateSetupIntentAsync()
    {
        try
        {
            var user = await GetCurrentUserAsync();
            var customer = await GetOrCreateCustomerForUserAsync(user);

            var setupIntentService = new SetupIntentService(_stripeClient);
            var setupIntent = await setupIntentService.CreateAsync(new SetupIntentCreateOptions
            {
                Customer = customer.Id,
                PaymentMethodTypes = new List<string> { "card" },
                Usage = "off_session" // Allow charging the card later without customer present
            });

            // Create customer session with payment_element enabled (modern approach)
            var customerSession = await CreateCustomerSessionWithPaymentElementAsync(customer.Id);

            // Create ephemeral key (legacy approach for older mobile SDKs)
            var ephemeralKeyService = new EphemeralKeyService(_stripeClient);
            var ephemeralKey = await ephemeralKeyService.CreateAsync(new EphemeralKeyCreateOptions
            {
                Customer = customer.Id,
                StripeVersion = "2024-12-18.acacia" // Current Stripe API version
            });

            return new SetupIntentResponse
            {
                ClientSecret = setupIntent.ClientSecret,
                CustomerId = customer.Id,
                CustomerSessionSecret = customerSession.ClientSecret,
                CustomerEphemeralKeySecret = ephemeralKey.Secret,
                PublishableKey = _publishableKey
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error creating setup intent: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating setup intent: {Message}", ex.Message);
            throw;
        }
    }


    public async Task<bool> HandleWebhookAsync(string json, string stripeSignature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _webhookSecret);

            _logger.LogInformation("Processing Stripe webhook event: {EventType} ({EventId})", stripeEvent.Type, stripeEvent.Id);

            // Idempotency: Stripe delivers at-least-once and retries non-2xx/timeouts.
            // If this event id was already processed, skip and return success so Stripe
            // stops retrying. This is the cheap common-case guard for sequential retries.
            var alreadyProcessed = await _dbContext.StripeWebhookEvents
                .AnyAsync(e => e.EventId == stripeEvent.Id);
            if (alreadyProcessed)
            {
                _logger.LogInformation("Duplicate Stripe event {EventId} ({Type}) ignored.", stripeEvent.Id, stripeEvent.Type);
                return true;
            }

            // Process the event and record it in the idempotency ledger atomically, so a
            // retry never re-applies a handler that already committed. The unique index on
            // EventId closes the race where two duplicate deliveries run concurrently.
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                        await HandlePaymentIntentSucceededAsync(paymentIntent);
                        break;

                    case "payment_intent.payment_failed":
                        var failedIntent = stripeEvent.Data.Object as PaymentIntent;
                        _logger.LogWarning("Payment failed for PaymentIntent: {Id}. Error: {Message}", failedIntent.Id, failedIntent.LastPaymentError?.Message);
                        break;

                    case "charge.refunded":
                        var charge = stripeEvent.Data.Object as Charge;
                        if (charge != null)
                        {
                            await HandleChargeRefundedAsync(charge);
                        }
                        break;

                    case "setup_intent.succeeded":
                        var setupIntent = stripeEvent.Data.Object as SetupIntent;
                        _logger.LogInformation("SetupIntent succeeded: {Id}", setupIntent.Id);
                        break;

                    default:
                        _logger.LogInformation("Unhandled event type: {Type}", stripeEvent.Type);
                        break;
                }

                _dbContext.StripeWebhookEvents.Add(new StripeWebhookEvent
                {
                    EventId = stripeEvent.Id,
                    EventType = stripeEvent.Type,
                    ProcessedAtUtc = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
                await tx.CommitAsync();

                return true;
            }
            catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
            {
                // A concurrent duplicate delivery already inserted the ledger row.
                await tx.RollbackAsync();
                _logger.LogInformation("Concurrent duplicate Stripe event {EventId} ({Type}) ignored.", stripeEvent.Id, stripeEvent.Type);
                return true;
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error in webhook: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// True when the <see cref="DbUpdateException"/> was caused by a PostgreSQL unique
    /// constraint violation (SQLSTATE 23505) — i.e. the Stripe event id was already
    /// recorded by a concurrent duplicate delivery.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505";

    private async Task HandlePaymentIntentSucceededAsync(PaymentIntent paymentIntent)
    {
        string? orderId = null;
        paymentIntent.Metadata?.TryGetValue("OrderId", out orderId);

        Order? order = null;
        if (!string.IsNullOrEmpty(orderId))
        {
            order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Uid == orderId);
        }

        if (order == null)
        {
            order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.RawRequest == paymentIntent.Id);
        }

        if (order != null)
        {
            if (order.OrderStatus != Core.Domain.Enums.OrderStatusEnum.Processing && 
                order.OrderStatus != Core.Domain.Enums.OrderStatusEnum.Completed)
            {
                order.OrderStatus = Core.Domain.Enums.OrderStatusEnum.Processing;
                order.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
                _logger.LogInformation("Order {OrderId} updated to Processing via webhook. (PI: {PaymentIntentId})", order.Uid, paymentIntent.Id);
            }
        }
        else
        {
            _logger.LogWarning("Order not found in database for PaymentIntent {Id} (OrderId metadata: {OrderId})", paymentIntent.Id, orderId ?? "N/A");
        }
    }

    private async Task HandleChargeRefundedAsync(Charge charge)
    {
        var paymentIntentId = charge.PaymentIntentId;

        if (string.IsNullOrEmpty(paymentIntentId))
        {
            _logger.LogWarning("Charge.refunded event received without PaymentIntentId. ChargeId: {ChargeId}", charge.Id);
            return;
        }

        _logger.LogInformation("Processing charge.refunded webhook for PaymentIntent: {PaymentIntentId}, Charge: {ChargeId}", paymentIntentId, charge.Id);

        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntentId);
        if (order == null)
        {
            order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.RawRequest == paymentIntentId);
        }

        if (order == null)
        {
            _logger.LogWarning("Order not found for refunded PaymentIntent: {PaymentIntentId}", paymentIntentId);
            return;
        }

        var orderItems = await _dbContext.OrderProductAffiliates
            .Where(opa => opa.OrderId == order.Id && opa.IsActive)
            .ToListAsync();

        var refundedItems = orderItems.Where(opa => opa.EscrowStatus == Core.Domain.Enums.EscrowStatusEnum.RefundInProgress).ToList();

        foreach (var item in refundedItems)
        {
            item.EscrowStatus = Core.Domain.Enums.EscrowStatusEnum.Refunded;
            item.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Order item {ItemUid} EscrowStatus updated to Refunded via charge.refunded webhook", item.Uid);
        }

        var allRefunded = orderItems.All(opa => opa.OrderItemStatus == Core.Domain.Enums.OrderStatusEnum.Refunded || opa.EscrowStatus == Core.Domain.Enums.EscrowStatusEnum.Refunded);
        if (allRefunded)
        {
            order.OrderStatus = Core.Domain.Enums.OrderStatusEnum.Refunded;
            order.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Order {OrderUid} status updated to Refunded via charge.refunded webhook", order.Uid);
        }

        await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
    }


    public async Task<RefundResponse> CreateRefundAsync(RefundRequest request)
    {
        try
        {
            var refundService = new RefundService(_stripeClient);

            var options = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentIntentId,
                Reason = request.Reason ?? "requested_by_customer"
            };

            if (request.AmountInCents.HasValue)
            {
                options.Amount = request.AmountInCents.Value;
            }

            if (request.Metadata != null)
            {
                options.Metadata = request.Metadata;
            }

            var refund = await refundService.CreateAsync(options);

            _logger.LogInformation("Stripe refund created: {RefundId} for PaymentIntent: {PaymentIntentId}, Amount: {Amount}",
                refund.Id, refund.PaymentIntentId, refund.Amount);

            return new RefundResponse
            {
                RefundId = refund.Id,
                PaymentIntentId = refund.PaymentIntentId,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Status = refund.Status,
                Reason = refund.Reason
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error creating refund for PaymentIntent {PaymentIntentId}: {Message}",
                request.PaymentIntentId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for PaymentIntent {PaymentIntentId}: {Message}",
                request.PaymentIntentId, ex.Message);
            throw;
        }
    }

    public async Task<TransferReversalResponse> ReverseTransferAsync(ReverseTransferRequest request)
    {
        try
        {
            var transferReversalService = new TransferReversalService(_stripeClient);

            var options = new TransferReversalCreateOptions();

            if (request.AmountInCents.HasValue)
            {
                options.Amount = request.AmountInCents.Value;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                options.Description = request.Description;
            }

            var reversal = await transferReversalService.CreateAsync(request.TransferId, options);

            _logger.LogInformation("Stripe transfer reversal created: {ReversalId} for Transfer: {TransferId}, Amount: {Amount}",
                reversal.Id, request.TransferId, reversal.Amount);

            return new TransferReversalResponse
            {
                ReversalId = reversal.Id,
                TransferId = request.TransferId,
                Amount = reversal.Amount,
                Currency = reversal.Currency
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe API error reversing transfer {TransferId}: {Message}",
                request.TransferId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reversing transfer {TransferId}: {Message}",
                request.TransferId, ex.Message);
            throw;
        }
    }

    #region Helper Methods

    private async Task<Customer> GetOrCreateCustomerAsync(string? customerId = null)
    {
        var customerService = new CustomerService(_stripeClient);

        if (!string.IsNullOrWhiteSpace(customerId))
        {
            return await customerService.GetAsync(customerId);
        }

        if (_currentUserService.IsUserLoggedIn())
        {
            var userId = _currentUserService.GetUserId();
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
                {
                    return await customerService.GetAsync(user.StripeCustomerId);
                }

                // Create new customer with user's email and name, save to DB
                var newCustomer = await customerService.CreateAsync(new CustomerCreateOptions
                {
                    Email = user.Email,
                    Name = user.FirstName?.Trim()
                });

                await SaveCustomerIdToUserAsync(userId, newCustomer.Id);
                return newCustomer;
            }
        }

        // Anonymous user - create customer without email/name
        return await customerService.CreateAsync(new CustomerCreateOptions());
    }

    private async Task<Customer> GetOrCreateCustomerForUserAsync(User user)
    {
        var customerService = new CustomerService(_stripeClient);

        if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            var customer = await customerService.GetAsync(user.StripeCustomerId);
            
            // Only update if email/name changed
            var userEmail = user.Email;
            var userName = user.FirstName?.Trim();
            
            if (customer.Email != userEmail || customer.Name != userName)
            {
                customer = await customerService.UpdateAsync(user.StripeCustomerId, new CustomerUpdateOptions
                {
                    Email = userEmail,
                    Name = userName
                });
            }
            
            return customer;
        }

        var newCustomer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.FirstName?.Trim()
        });

        await SaveCustomerIdToUserAsync(user.Id, newCustomer.Id);
        return newCustomer;
    }

    private async Task<CustomerSession> CreateCustomerSessionAsync(string customerId)
    {
        var customerSessionService = new CustomerSessionService(_stripeClient);
        return await customerSessionService.CreateAsync(new CustomerSessionCreateOptions
        {
            Customer = customerId,
            Components = new CustomerSessionComponentsOptions
            {
                MobilePaymentElement = new CustomerSessionComponentsMobilePaymentElementOptions
                {
                    Enabled = true,
                    Features = new CustomerSessionComponentsMobilePaymentElementFeaturesOptions
                    {
                        PaymentMethodSave = "enabled",
                        PaymentMethodRedisplay = "enabled",
                        PaymentMethodRemove = "enabled"
                    }
                }
            }
        });
    }

    private async Task<CustomerSession> CreateCustomerSessionWithPaymentElementAsync(string customerId)
    {
        var customerSessionService = new CustomerSessionService(_stripeClient);
        return await customerSessionService.CreateAsync(new CustomerSessionCreateOptions
        {
            Customer = customerId,
            Components = new CustomerSessionComponentsOptions
            {
                PaymentElement = new CustomerSessionComponentsPaymentElementOptions
                {
                    Enabled = true,
                    Features = new CustomerSessionComponentsPaymentElementFeaturesOptions
                    {
                        PaymentMethodSave = "enabled",
                        PaymentMethodRedisplay = "enabled",
                        PaymentMethodRemove = "enabled"
                    }
                }
            }
        });
    }

    private async Task<User> GetCurrentUserAsync()
    {
        if (!_currentUserService.IsUserLoggedIn())
        {
            throw new UnauthorizedAccessException("User must be logged in.");
        }

        var user = await _currentUserService.GetUserAsync(skipDetails: true);
        if (user == null)
        {
            throw new ArgumentException("User not found.");
        }

        return user;
    }

    private async Task<User> GetCurrentUserWithStripeCustomerAsync()
    {
        var user = await GetCurrentUserAsync();
        
        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            throw new ArgumentException("User does not have a Stripe customer. Please create a customer session first.");
        }

        return user;
    }

    private async Task SaveCustomerIdToUserAsync(string userId, string customerId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null)
        {
            user.StripeCustomerId = customerId;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(System.Threading.CancellationToken.None);
            _logger.LogInformation("Saved Stripe customer ID {CustomerId} to user {UserId}", customerId, userId);
        }
    }

    private async Task EnsureCustomerHasEmailAsync(string customerId, string? userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return;
        }

        var customerService = new CustomerService(_stripeClient);
        var customer = await customerService.GetAsync(customerId);
        
        if (string.IsNullOrWhiteSpace(customer.Email))
        {
            await customerService.UpdateAsync(customerId, new CustomerUpdateOptions { Email = userEmail });
            _logger.LogInformation("Updated Stripe customer {CustomerId} with email {Email}", customerId, userEmail);
        }
    }

    private static bool ValidateCardDetails(SaveCardRequest request, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(request.CardNumber))
        {
            error = "Card number is required.";
            return false;
        }

        if (request.ExpMonth < 1 || request.ExpMonth > 12)
        {
            error = "Expiration month must be between 1 and 12.";
            return false;
        }

        if (request.ExpYear < DateTime.Now.Year)
        {
            error = "Expiration year must be in the future.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Cvc))
        {
            error = "CVC is required.";
            return false;
        }

        return true;
    }

    private async Task<PaymentIntent> CreatePaymentIntentWithRetryAsync(
        PaymentIntentService paymentIntentService,
        PaymentIntentCreateOptions options)
    {
        try
        {
            return await paymentIntentService.CreateAsync(options);
        }
        catch (StripeException ex) when (
            ex.HttpStatusCode == System.Net.HttpStatusCode.RequestTimeout ||
            ex.Message.Contains("timeout") ||
            ex.Message.Contains("connection") ||
            ex.StripeError?.Type == "api_connection_error")
        {
            _logger.LogWarning("Stripe timeout/connection error, retrying once: {Message}", ex.Message);
            await Task.Delay(1000);
            return await paymentIntentService.CreateAsync(options);
        }
    }

    #endregion
}
