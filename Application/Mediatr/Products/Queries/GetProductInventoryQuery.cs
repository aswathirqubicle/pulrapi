using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Models;
using Core.Application.Models.Products;
using System.Linq;

namespace Core.Application.Mediatr.Products.Queries
{
    public class GetProductInventoryQuery : IRequest<PagingResponse<ProductInventoryResponse>>
    {
        public PagingParamsRequest PagingParams { get; set; }
        [Required]
        public string StoreUid { get; set; }
    }

    public class GetProductInventoryQueryHandler : IRequestHandler<GetProductInventoryQuery, PagingResponse<ProductInventoryResponse>>
    {
        private readonly ILogger<GetProductInventoryQueryHandler> _logger;
        private readonly IStoreService _storeService;

        public GetProductInventoryQueryHandler(ILogger<GetProductInventoryQueryHandler> logger,
            IStoreService storeService)
        {
            _logger = logger;
            _storeService = storeService;
        }

        public async Task<PagingResponse<ProductInventoryResponse>> Handle(GetProductInventoryQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var productsPagedResponse = await _storeService.GetProductInventoryList(request.PagingParams, request.StoreUid);
                productsPagedResponse.ItemIds = productsPagedResponse.Items.Select(item => item.Uid).ToList();
                return productsPagedResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
