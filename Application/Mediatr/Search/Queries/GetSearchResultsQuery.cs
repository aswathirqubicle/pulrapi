using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Search.Notifications;
using Core.Application.Models.Search;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Search.Queries;

public class GetSearchResultsQuery : IRequest<SearchResult>
{
    public string Query { get; set; }
    public int ResultCount { get; set; } = 5;
}

public class GetSearchResultsQueryHandler : IRequestHandler<GetSearchResultsQuery, SearchResult>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ILogger<GetSearchResultsQueryHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GetSearchResultsQueryHandler(IApplicationDbContext dbContext, 
        IMediator mediator, ILogger<GetSearchResultsQueryHandler> logger,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<SearchResult> Handle(GetSearchResultsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var searchResult = new SearchResult();
            if (String.IsNullOrWhiteSpace(request.Query))
                return searchResult;

            var cUser = await _currentUserService.GetUserAsync();

            var myProfileUid = cUser?.Profile?.Uid;
            var blockedProfileIds = new List<string>();
            if (myProfileUid != null)
            {
                blockedProfileIds = await _dbContext.UserBlocks
                    .Where(ub => (ub.BlockerProfileId == myProfileUid || ub.BlockedProfileId == myProfileUid) && ub.IsActive)
                    .Select(ub => ub.BlockerProfileId == myProfileUid ? ub.BlockedProfileId : ub.BlockerProfileId)
                    .ToListAsync(cancellationToken);
            }

            var postsQuery = _dbContext.Posts
                .Where(p => p.IsActive
                            && !p.User.IsSuspended
                            && p.Text.ToLower().Contains(request.Query.ToLower().Trim())
                            && p.MediaFile.IsActive);

            if (cUser == null)
            {
                postsQuery = postsQuery.Where(p =>
                    p.User.Profile.ProfileSettings == null ||
                    p.User.Profile.ProfileSettings.IsProfilePublic);
            }
            else
            {
                var currentProfileId = cUser.Profile.Id;
                postsQuery = postsQuery.Where(p =>
                    (p.User.Profile.ProfileSettings == null || p.User.Profile.ProfileSettings.IsProfilePublic) ||
                    p.User.Id == cUser.Id ||
                    _dbContext.ProfileFollowers.Any(pf =>
                        pf.ProfileId == p.User.Profile.Id && pf.FollowerId == currentProfileId));
            }

            searchResult.Posts = await postsQuery
                .Where(p => !blockedProfileIds.Contains(p.User.Profile.Uid))
                .Select(p =>
                    new BaseSearchResult
                    {
                        Uid = p.Uid,
                        Name = p.Text,
                        ImageUrl = p.MediaFile.Url,
                        ThumbnailUrl = string.IsNullOrEmpty(p.ThumbnailUrl) ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null) : p.ThumbnailUrl,
                    }).Take(request.ResultCount).ToListAsync(cancellationToken);


            searchResult.Products = await _dbContext.Products
                .Include(p => p.Country)
                .Where(p => p.IsActive
                            && (p.Name.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.WhatIsIt.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                (p.Brand != null && p.Brand.ToLower().Contains(request.Query.ToLower().Trim()))))
                .Select(p =>
                    new ProductSearchResult
                    {
                        ProductUid = p.Uid,
                        ProductName = p.Name,
                        ProductImageUrl = p.ProductMediaFiles
                            .Where(pmf => pmf.MediaFile.IsActive)
                            .OrderBy(mf => mf.MediaFile.Priority)
                            .Select(mf => mf.MediaFile.Url).FirstOrDefault(),
                        WhatIsIt = p.WhatIsIt,
                        Brand = p.Brand,
                        MinPrice = p.MinPrice,
                        MaxPrice = p.MaxPrice,
                        CountryCode = p.Country != null ? p.Country.Iso3 : null,
                        CurrencyCode = p.Country != null ? p.Country.Iso4 : null
                    }).Take(request.ResultCount).ToListAsync(cancellationToken);

            searchResult.Profiles = await _dbContext.Profiles
                .Where(p => !p.User.IsSuspended
                            && !blockedProfileIds.Contains(p.Uid)
                            && (p.User.FirstName.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.User.LastName.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.About.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.User.CityName.ToLower().Contains(request.Query.ToLower().Trim()) ||
                                p.User.Country.Name.ToLower().Contains(request.Query.ToLower().Trim())
                            ))
                .Select(p =>
                    new ProfileSearchResult
                    {
                        Uid = p.User.Id,
                        Username = p.User.UserName,
                        FullName = p.User.FirstName,
                        Name = p.User.FirstName,
                        ImageUrl = p.User.Profile.ImageUrl
                    }).Take(request.ResultCount).ToListAsync(cancellationToken);

            //searchResult.Stores = await _dbContext.Stores
            //    .Where(s => !s.User.IsSuspended
            //                && (s.Name.ToLower().Contains(request.Query.ToLower().Trim()) ||
            //                    s.Description.ToLower().Contains(request.Query.ToLower().Trim()) ||
            //                    s.LegalName.ToLower().Contains(request.Query.ToLower().Trim()) ||
            //                    s.UniqueName.ToLower().Contains(request.Query.ToLower().Trim())
            //                ))
            //    .Select(s =>
            //        new StoreSearchResult
            //        {
            //            Uid = s.Uid,
            //            Name = s.Name,
            //            UniqueName = s.UniqueName,
            //            ImageUrl = s.ImageUrl
            //        }).Take(request.ResultCount).ToListAsync(cancellationToken);

            if(await _currentUserService.GetUserAsync() != null)
            {
                await _mediator.Publish(new CreateSearchHistoryEntryNotification {Term = request.Query}, cancellationToken);
            }

            return searchResult;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting search results");
            throw;
        }
    }
}