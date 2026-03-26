using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Stores;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Stores.Queries;

public class GetStoresDetailsQuery : IRequest<StoreDetailsResponse>
{
    public string? StoreUid { get; set; }
}

public class GetStoresDetailsQueryHandler : IRequestHandler<GetStoresDetailsQuery, StoreDetailsResponse>
{
    private readonly ILogger<GetStoresDetailsQueryHandler> _logger;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public GetStoresDetailsQueryHandler(ILogger<GetStoresDetailsQueryHandler> logger, IApplicationDbContext dbContext,
        IMapper mapper, IMediator mediator)
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<StoreDetailsResponse> Handle(GetStoresDetailsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var store = await _dbContext.Stores.Where(s => s.Uid == request.StoreUid).ProjectTo<StoreDetailsResponse>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(cancellationToken);

                if (store == null) { throw new NotFoundException("Requested store is not found"); }
                    
            return store;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting store details");
            throw;
        }
    }
}