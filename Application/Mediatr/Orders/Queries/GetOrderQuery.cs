using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Orders.Queries;
using Core.Application.Models.Orders;
using Core.Application.Exceptions;
using Core.Application.Models.Currencies;
using Core.Application.Models.ShippingDetails;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;
using Core.Domain.Enums;
using Newtonsoft.Json;

namespace Core.Application.Mediatr.Orders.Queries
{
    public class GetOrderQuery : IRequest<OrderDetailsResponse>
    {
        [Required] public string Uid { get; set; }
    }

    public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, OrderDetailsResponse>
    {
        private readonly IOrderService _orderService;
        private readonly ICurrentUserService _currentUserService;

        public GetOrderQueryHandler(IOrderService orderService, ICurrentUserService currentUserService)
        {
            _orderService = orderService;
            _currentUserService = currentUserService;
        }

        public async Task<OrderDetailsResponse> Handle(GetOrderQuery request, CancellationToken cancellationToken)
        {
            var user = await _currentUserService.GetUserAsync(skipDetails: true);
            if (user == null) throw new NotAuthenticatedException("User must be logged in.");

            return await _orderService.GetOrderDetailsAsync(user.Id, request.Uid, cancellationToken);
        }
    }
}
