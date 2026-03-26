using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Stores.Queries;
using Core.Application.Models;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Stores.Queries
{
    public class GetStoresQuery : PagingParamsRequest, IRequest<PagingResponse<StoreResponse>>
    {
        public StoreSortingLogicEnum StoreSortingLogic { get; set; }
    }

    public class GetStoresQueryHandler : IRequestHandler<GetStoresQuery, PagingResponse<StoreResponse>>
    {
        private readonly ILogger<GetStoresQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IQueryHelperService _queryHelperService;
        private readonly ICurrentUserService _currentUserService;

        public GetStoresQueryHandler(
            ILogger<GetStoresQueryHandler> logger,
            IApplicationDbContext dbContext,
            IMapper mapper,
            IQueryHelperService queryHelperService,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
            _queryHelperService = queryHelperService;
            _currentUserService = currentUserService;
        }

        public async Task<PagingResponse<StoreResponse>> Handle(GetStoresQuery request, CancellationToken cancellationToken)
        {
            try
            {
                 var cUser = await _currentUserService.GetUserAsync();

                IQueryable<Store> query = _dbContext.Stores.AsNoTracking();

                query = query.Where(e => e.IsActive == true && !e.User.IsSuspended);
                // TODO search
                //query = SearchFilter(request.Search, query);
                
                if (!String.IsNullOrWhiteSpace(request.Search))
                {
                    query = query.Where(s => 
                        s.Name.ToLower().Contains(request.Search.Trim().ToLower()) ||
                        s.Description.ToLower().Contains(request.Search.Trim().ToLower()) ||
                        s.LegalName.ToLower().Contains(request.Search.Trim().ToLower()) ||
                        s.UniqueName.ToLower().Contains(request.Search.Trim().ToLower())
                    );
                }

                if (request.StoreSortingLogic == StoreSortingLogicEnum.Trending)
                {
                    query = query.OrderByDescending(e => e.LikesCount);
                }

                if (String.IsNullOrWhiteSpace(request.Order) || String.IsNullOrWhiteSpace(request.OrderBy))
                {
                    query = query.OrderByDescending(u => u.Id);
                }
                else
                {
                    query = _queryHelperService.AppendOrderBy(query, request.OrderBy, request.Order);
                }
               

                var queryMapped = query.Select(c => new StoreResponse()
                {
                    Uid = c.Uid,
                    UniqueName = c.UniqueName,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl,
                    Followers = c.StoreFollowers.Count(),
                    LikesCount = c.LikesCount,
                    ProductsCount = c.Products.Where(p => p.IsActive).Count(),
                });

                var list = await PagedList<StoreResponse>.ToPagedListAsync(queryMapped, request.PageNumber, request.PageSize);

                if (list.Any() && cUser != null)
                {
                    List<string> storeUids = list.Select(s => s.Uid).ToList();

                    var myFollows = await _dbContext.StoreFollowers
                        .Where(sf => sf.FollowerId == cUser.Profile.Id && storeUids.Contains(sf.Store.Uid))
                        .Select(sf => sf.Store.Uid).ToListAsync(cancellationToken);

                    foreach (var item in list)
                    {
                        item.FollowedByMe = myFollows.Contains(item.Uid);
                    }
                }

                var storesPagedResponse = _mapper.Map<PagingResponse<StoreResponse>>(list);
                storesPagedResponse.ItemIds = storesPagedResponse.Items.Select(item => item.Uid).ToList();
                return storesPagedResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
