using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Core.Application.Models;
using Core.Application.Interfaces;
using AutoMapper.QueryableExtensions;
using Core.Application.Models.Products;
using Core.Application.Mappings;
using Core.Domain.Entities;

namespace Dashboard.Application.Mediatr.Products.Queries;

public class GetProductsInventoryQuery : PagingParamsRequest, IRequest<PagingResponse<ProductInventoryResponse>>
{
    public string? StoreUid { get; set; }
}

public class GetProductsInventoryQueryHandler : IRequestHandler<GetProductsInventoryQuery, PagingResponse<ProductInventoryResponse>>
{
    private readonly ILogger<GetProductsInventoryQueryHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetProductsInventoryQueryHandler(ILogger<GetProductsInventoryQueryHandler> logger, IApplicationDbContext dbContext, IMapper mapper)
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public Task<PagingResponse<ProductInventoryResponse>> Handle(GetProductsInventoryQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    //public async Task<PagingResponse<ProductInventoryResponse>> Handle(GetProductsInventoryQuery request, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        IQueryable<Product> query = _dbContext.Products;

    //        if (!String.IsNullOrWhiteSpace(request.StoreUid))
    //        {
    //            query = query.Where(q => q.Store.Uid == request.StoreUid);
    //        }
    //        var list = await query.OrderByDescending(x => x.CreatedAt)
    //                              .ProjectTo<ProductInventoryResponse>(_mapper.ConfigurationProvider)
    //                              .PaginatedListAsync(request.PageNumber, request.PageSize);

    //        return _mapper.Map<PagingResponse<ProductInventoryResponse>>(list);
    //    }
    //    catch (Exception e)
    //    {
    //        _logger.LogError(e, e.Message);
    //        throw;
    //    }
    //}
}
