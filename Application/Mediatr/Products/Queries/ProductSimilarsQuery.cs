using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Products.Queries;
using Core.Application.Models.Products;

namespace Core.Application.Mediatr.Products.Queries
{
    public class ProductSimilarsQuery : IRequest<ProductSimilarsResponse>
    {
        [Required]
        public string ProductUid { get; set; }
    }

    public class ProductSimilarsQueryHandler : IRequestHandler<ProductSimilarsQuery, ProductSimilarsResponse>
    {
        private readonly ILogger<ProductSimilarsQueryHandler> _logger;
        private readonly IStoreService _storeService;

        public ProductSimilarsQueryHandler(ILogger<ProductSimilarsQueryHandler> logger,
            IStoreService storeService)
        {
            _logger = logger;
            _storeService = storeService;
        }

        public async Task<ProductSimilarsResponse> Handle(ProductSimilarsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await _storeService.GetProductSimilars(request.ProductUid);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
