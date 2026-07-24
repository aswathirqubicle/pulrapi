using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Orders.Queries.GetShippingDetails;

public class GetShippingDetailsQuery : IRequest<List<ItemShippingStatusResponse>>
{
    public string OrderUid { get; set; }
    public string ItemUid { get; set; }
}

public class GetShippingDetailsQueryHandler : IRequestHandler<GetShippingDetailsQuery, List<ItemShippingStatusResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetShippingDetailsQueryHandler> _logger;

    public GetShippingDetailsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<GetShippingDetailsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<List<ItemShippingStatusResponse>> Handle(GetShippingDetailsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
                throw new NotAuthenticatedException("User must be logged in.");

            var profile = await _dbContext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            var order = await _dbContext.Orders
                .Include(o => o.OrderProductAffiliates)
                    .ThenInclude(opa => opa.Product)
                .FirstOrDefaultAsync(o => o.Uid == request.OrderUid && o.IsActive, cancellationToken);

            if (order == null)
                throw new NotFoundException("Order not found.");

            bool isBuyer = profile != null && order.ProfileId == profile.Id;

            var items = order.OrderProductAffiliates.AsEnumerable();

            if (!isBuyer)
                items = items.Where(opa => opa.Product != null && opa.Product.UserId == user.Id);

            if (!string.IsNullOrWhiteSpace(request.ItemUid))
                items = items.Where(opa => opa.Uid == request.ItemUid);

            var itemList = items.ToList();

            if (!itemList.Any())
                throw new NotFoundException("Order not found or access denied.");

            var itemIds = itemList.Select(opa => opa.Id).ToList();

            var proofsByItemId = await _dbContext.OrderItemShippingProofs
                .Include(sp => sp.MediaFile)
                .Where(sp => itemIds.Contains(sp.OrderProductAffiliateId) && sp.IsActive)
                .ToListAsync(cancellationToken);

            var proofLookup = proofsByItemId
                .GroupBy(sp => sp.OrderProductAffiliateId)
                .ToDictionary(g => g.Key, g => g.OrderBy(sp => sp.Priority).ToList());

            return itemList.Select(opa => new ItemShippingStatusResponse
            {
                OrderUid = order.Uid,
                ItemUid = opa.Uid,
                ProductName = opa.ProductNameSnapshot,
                PrimaryImageUrl = opa.PrimaryImageUrlSnapshot,
                IsShipped = opa.ShippedAt.HasValue,
                TrackingNumber = opa.TrackingNumber,
                ShippingProvider = opa.ShippingProvider,
                ShippedAt = opa.ShippedAt,
                ShippingProofMediaFileURLs = proofLookup.TryGetValue(opa.Id, out var proofs)
                    ? proofs
                        .Where(sp => sp.MediaFile?.Url != null)
                        .Select(sp => sp.MediaFile.Url)
                        .ToList()
                    : new List<string>()
            }).ToList();
        }
        catch (Exception ex) when (ex is not NotAuthenticatedException and not NotFoundException)
        {
            _logger.LogError(ex, "Error getting shipping details for order {OrderUid}: {Message}", request.OrderUid, ex.Message);
            throw;
        }
    }
}
