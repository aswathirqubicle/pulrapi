using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Orders;
using Core.Application.Models.ShippingDetails;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;
using Core.Domain.Enums;
using Newtonsoft.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Orders.Queries
{
    public class GetAllOrdersByStoreQuery : PagingParamsRequest, IRequest<PagingResponse
        <OrderResponse>>
    {
        [Required] public string StoreUid { get; set; }
    }

    public class GetAllOrdersByStoreQueryHandler : IRequestHandler<GetAllOrdersByStoreQuery, PagingResponse<OrderResponse>>
    {
        private readonly IOrderService _orderService;
        private readonly ICurrentUserService _currentUserService;

        public GetAllOrdersByStoreQueryHandler(IOrderService orderService, ICurrentUserService currentUserService)
        {
            _orderService = orderService;
            _currentUserService = currentUserService;
        }

        public async Task<PagingResponse<OrderResponse>> Handle(GetAllOrdersByStoreQuery request,
            CancellationToken cancellationToken)
        {
            var cUser = await _currentUserService.GetUserAsync(false, true);
            // In the new model, StoreUid is effectively ignored in favor of the authenticated seller's ID
            return await _orderService.GetAllOrdersBySellerAsync(cUser.Id, request.PageNumber, request.PageSize, cancellationToken);
        }
    }
}
