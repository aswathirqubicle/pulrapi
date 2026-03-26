using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core.Application.Interfaces;
using Core.Application.Mappings;
using Core.Application.Models;
using Dashboard.Application.Mediatr.Stores.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dashboard.Application.Mediatr.Stores.Queries;

public class GetStoresQuery : PagingParamsRequest, IRequest<PagingResponse<StoreResponse>>
{
    public string? UserId { get; set; }
}

public class GetStoresQueryHandler : IRequestHandler<GetStoresQuery, PagingResponse<StoreResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStoresQueryHandler> _logger;

    public GetStoresQueryHandler(IApplicationDbContext dbContext, IMapper mapper, ILogger<GetStoresQueryHandler> logger)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagingResponse<StoreResponse>> Handle(GetStoresQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var storesQuery = _dbContext.Stores.Where(s => s.IsActive);

            if (!String.IsNullOrEmpty(request.UserId))
            {
                storesQuery = storesQuery.Where(s => s.UserId == request.UserId);
            }

            if (!String.IsNullOrEmpty(request.Search))
            {
                storesQuery = storesQuery.Where(s =>
                    s.Name.ToLower().Trim().Contains(request.Search.ToLower().Trim()) ||
                    (s.User.UserName != null &&
                     s.User.UserName.ToLower().Trim().Contains(request.Search.ToLower().Trim())) ||
                    s.LegalName.ToLower().Trim().Contains(request.Search.ToLower().Trim()) ||
                    s.UniqueName.ToLower().Trim().Contains(request.Search.ToLower().Trim()));
            }

            storesQuery = storesQuery.Include(s => s.Products);

            var storesRes = await storesQuery.ProjectTo<StoreResponse>(_mapper.ConfigurationProvider)
                .PaginatedListAsync(request.PageNumber, request.PageSize);

            return _mapper.Map<PagingResponse<StoreResponse>>(storesRes);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting user stores");
            throw;
        }
    }
}