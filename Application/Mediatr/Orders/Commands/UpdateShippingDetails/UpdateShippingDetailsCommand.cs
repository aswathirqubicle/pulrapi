using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Orders.Commands.UpdateShippingDetails;

public class UpdateShippingDetailsCommand : IRequest<bool>
{
    public List<string> ItemUids { get; set; } = new();
    public string TrackingNumber { get; set; }
    public string ShippingProvider { get; set; }
    public List<string> ShippingProofMediaFileUids { get; set; } = new();
}

public class UpdateShippingDetailsCommandHandler : IRequestHandler<UpdateShippingDetailsCommand, bool>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateShippingDetailsCommandHandler> _logger;

    public UpdateShippingDetailsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<UpdateShippingDetailsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateShippingDetailsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null)
                throw new NotAuthenticatedException("User must be logged in.");

            // Resolve media file UIDs to IDs
            List<int> proofMediaFileIds = new();
            if (request.ShippingProofMediaFileUids != null && request.ShippingProofMediaFileUids.Any())
            {
                var mediaFiles = await _dbContext.MediaFiles
                    .Where(mf => request.ShippingProofMediaFileUids.Contains(mf.Uid) && mf.IsActive)
                    .ToListAsync(cancellationToken);

                if (mediaFiles.Count != request.ShippingProofMediaFileUids.Count)
                    throw new NotFoundException("One or more shipping proof media files were not found.");

                proofMediaFileIds = mediaFiles.Select(mf => mf.Id).ToList();
            }

            var orderItems = await _dbContext.OrderProductAffiliates
                .Include(opa => opa.Product)
                .Include(opa => opa.ShippingProofs)
                .Where(opa => request.ItemUids.Contains(opa.Uid) && opa.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var item in orderItems)
            {
                if (item.Product == null || item.Product.UserId != user.Id)
                    throw new ForbiddenException("You are not authorized to update this order item.");

                item.TrackingNumber = request.TrackingNumber;
                item.ShippingProvider = request.ShippingProvider;
                item.UpdatedAt = DateTime.UtcNow;

                // Replace existing proof images
                if (item.ShippingProofs != null && item.ShippingProofs.Any())
                    _dbContext.OrderItemShippingProofs.RemoveRange(item.ShippingProofs);

                for (int i = 0; i < proofMediaFileIds.Count; i++)
                {
                    _dbContext.OrderItemShippingProofs.Add(new OrderItemShippingProof
                    {
                        OrderProductAffiliateId = item.Id,
                        MediaFileId = proofMediaFileIds[i],
                        Priority = i
                    });
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Shipping details updated for items {ItemUids} by user {UserId}",
                string.Join(", ", request.ItemUids), user.Id);

            return true;
        }
        catch (Exception ex) when (ex is not NotAuthenticatedException and not NotFoundException and not ForbiddenException)
        {
            _logger.LogError(ex, "Error updating shipping details for items {ItemUids}: {Message}",
                string.Join(", ", request.ItemUids), ex.Message);
            throw;
        }
    }
}
