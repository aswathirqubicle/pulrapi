using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Orders;
using Core.Application.Models.ShippingDetails;
using Core.Application.Models.MediaFiles;
using Core.Application.Models.Profiles;
using Core.Application.Helpers;
using Core.Domain.Enums;
using Newtonsoft.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Orders.Queries;

public class GetUserOrdersQuery : PagingParamsRequest, IRequest<PagingResponse<OrderResponse>>
{
    public bool CheckProcessingOnly { get; set; }
}




public class GetUserOrdersQueryHandler : IRequestHandler<GetUserOrdersQuery, PagingResponse<OrderResponse>>
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public GetUserOrdersQueryHandler(IOrderService orderService, ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    public async Task<PagingResponse<OrderResponse>> Handle(GetUserOrdersQuery request, CancellationToken cancellationToken)
    {
        var user = await _currentUserService.GetUserAsync(skipDetails: true);
        if (user == null) throw new NotAuthenticatedException("User must be logged in.");

        return await _orderService.GetUserOrdersAsync(user.Id, request.PageNumber, request.PageSize, request.CheckProcessingOnly, cancellationToken);
    }
}
