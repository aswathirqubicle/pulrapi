using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.ShippingDetails.Queries;
using Core.Application.Models;
using Core.Application.Models.ShippingDetails;

namespace Core.Application.Mediatr.ShippingDetails.Queries
{
    public class GetShippingDetailsQuery : PagingParamsRequest, IRequest<PagingResponse<ShippingDetailsResponse>>
    {
        /// <summary>
        /// Optional filter: if null, returns all addresses. If true, returns only billing addresses. If false, returns only shipping addresses.
        /// </summary>
        public bool? IsBillingAddress { get; set; }
    }

    public class GetShippingDetailsQueryHandler : IRequestHandler<GetShippingDetailsQuery, PagingResponse<ShippingDetailsResponse>>
    {
        private readonly ILogger<GetShippingAddressQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetShippingDetailsQueryHandler(ILogger<GetShippingAddressQueryHandler> logger,
            IApplicationDbContext dbContext, ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<PagingResponse<ShippingDetailsResponse>> Handle(GetShippingDetailsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync(skipDetails: true);
                if (cUser == null)
                {
                    throw new NotAuthenticatedException("");
                }

                var query = _dbContext.ShippingDetails
                    .Include(sd => sd.CountryNavigation)
                    .Where(sd => sd.IsActive && sd.UserId == cUser.Id);

                // Apply optional filter by address type
                if (request.IsBillingAddress.HasValue)
                {
                    query = query.Where(sd => sd.IsBillingAddress == request.IsBillingAddress.Value);
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                var responseItems = items.Select(i => ShippingDetailsResponse.MapFromEntity(i)!).ToList();

                return new PagingResponse<ShippingDetailsResponse>
                {
                    Items = responseItems,
                    CurrentPage = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                    ItemIds = null
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
