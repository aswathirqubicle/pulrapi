using MediatR;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Application.Models.Products;
using Core.Application.Interfaces;
using Core.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Application.Mediatr.Products.Queries;

public class GetProductQuery : IRequest<ProductDetailsResponse>
{
    [Required]
    public string? ProductUid { get; set; }
}

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDetailsResponse>
{
    private readonly ILogger<GetProductQueryHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetProductQueryHandler(ILogger<GetProductQueryHandler> logger, IApplicationDbContext dbContext, IMapper mapper)
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ProductDetailsResponse> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _dbContext.Products.Where(e => e.Uid == request.ProductUid)
                                                                    .ProjectTo<ProductDetailsResponse>(_mapper.ConfigurationProvider)
                                                                    .FirstOrDefaultAsync();
            if(product == null){
                throw new NotFoundException();
            }

            return product;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}
