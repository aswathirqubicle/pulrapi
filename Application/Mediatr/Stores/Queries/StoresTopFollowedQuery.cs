using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Application.Interfaces;
using Core.Application.Models.Stores;
using System.Linq.Expressions;
using Core.Domain.Entities;

namespace Core.Application.Mediatr.Stores.Queries
{
    public class GetTopBrandsQuery : IRequest<List<StoreDetailsResponse>>
    {
        [Range(1, 20)] public int Count { get; set; }

        public List<string> StoreUidsToSkip { get; set; } = new List<string>();
    }

    public class GetTopBrandsQueryHandler : IRequestHandler<GetTopBrandsQuery, List<StoreDetailsResponse>>
    {
        private readonly ILogger<GetTopBrandsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public GetTopBrandsQueryHandler(
            ILogger<GetTopBrandsQueryHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<List<StoreDetailsResponse>> Handle(GetTopBrandsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                IQueryable<Store> storeQuery = _dbContext.Stores;

                if(request.StoreUidsToSkip.Count > 0)
                {
                    storeQuery = storeQuery.Where(s => !request.StoreUidsToSkip.Contains(s.Uid));
                }

                Expression<Func<Store, bool>> predicate = store => store.IsActive == true;
                if (cUser?.Profile != null) {
                    predicate = store => store.StoreFollowers.Where(s => s.FollowerId == cUser.Profile.Id).Any() == false;
                }

                var topFollowedList = await storeQuery
                    .Where(predicate)
                    .OrderByDescending(s => s.StoreFollowers.Count())
                    .Take(request.Count).Select(s => new StoreDetailsResponse()
                    {
                        Description = s.Description,
                        Name = s.Name,
                        UniqueName = s.UniqueName,
                        Followers = s.StoreFollowers.Count(),
                        Location = s.Location,
                        ImageUrl = s.ImageUrl,
                        Uid = s.Uid,
                        IsStore = true
                    }).ToListAsync(cancellationToken);

                if (topFollowedList.Any() && cUser != null)
                {
                    List<string> topFollowedListUids = topFollowedList.Select(s => s.Uid).ToList();

                    var myFollows = await _dbContext.StoreFollowers
                        .Where(sf => sf.FollowerId == cUser.Profile.Id && topFollowedListUids.Contains(sf.Store.Uid))
                        .Select(sf => sf.Store.Uid).ToListAsync(cancellationToken);

                    foreach (var item in topFollowedList)
                    {
                        item.FollowedByMe = myFollows.Contains(item.Uid);
                    }
                }

                return topFollowedList;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
