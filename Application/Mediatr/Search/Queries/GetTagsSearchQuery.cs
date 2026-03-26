using System;
using System.Threading;
using System.Threading.Tasks;
using Application.DTOs.Search;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Search.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Core.Application.Mediatr.Search.Queries
{
    public class GetTagsSearchQuery : IRequest<PaginatedResultDto<TagSearchResultDto>>
    {
        public string SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int? PageSize { get; set; }
    }

    public class GetTagsSearchQueryHandler : IRequestHandler<GetTagsSearchQuery, PaginatedResultDto<TagSearchResultDto>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMediator _mediator;

        public GetTagsSearchQueryHandler(IApplicationDbContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task<PaginatedResultDto<TagSearchResultDto>> Handle(GetTagsSearchQuery request, CancellationToken cancellationToken)
        {
            try
            {
                //await _mediator.Publish(new CreateSearchHistoryEntryNotification 
                //{ 
                //    Term = request.SearchTerm 
                //}, cancellationToken);

                // Create the base query
                var baseQuery = _dbContext.Hashtags
                    .Where(h => (h.Value.ToLower().StartsWith(request.SearchTerm.ToLower()) ||
                               h.Value.ToLower().Contains(request.SearchTerm.ToLower()))
                    && h.PostHashtags.Count > 0);

                // Get total count
                var totalCount = await baseQuery.CountAsync(cancellationToken);

                // Get paginated results
                var items = await baseQuery
                    .OrderByDescending(h => h.Value.ToLower().StartsWith(request.SearchTerm.ToLower()))
                    .ThenByDescending(h => h.PostHashtags.Count)  
                    .ThenBy(h => h.Value)                                   
                    .Select(h => new TagSearchResultDto
                    {
                        //Uid = h.Uid,
                        Value = h.Value,
                        Count = h.PostHashtags.Count
                    })
                    .Skip((request.Page - 1) * (request.PageSize ?? totalCount))
                    .Take(request.PageSize ?? totalCount)
                    .ToListAsync(cancellationToken);

                return PaginatedResultDto<TagSearchResultDto>.Create(
                    request.Page,
                    request.PageSize ?? totalCount,
                    totalCount,
                    items);
            }
            catch (Exception ex)
            {
                throw new Exception("Error searching tags", ex);
            }
        }
    }
}