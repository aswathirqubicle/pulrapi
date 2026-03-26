using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Constants;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Stores.Queries
{
    public class GetStoreDetailsQuery : IRequest<StoreDetailsResponse>
    {
        [Required]
        public string Uid { get; set; }
    }

    public class GetStoreDetailsQueryHandler : IRequestHandler<GetStoreDetailsQuery, StoreDetailsResponse>
    {
        private readonly ILogger<GetStoreDetailsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IApplicationDbContext _dbContext;

        public GetStoreDetailsQueryHandler(
            ILogger<GetStoreDetailsQueryHandler> logger,
            ICurrentUserService currentUserService,
            IApplicationDbContext dbContext
        )
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _dbContext = dbContext;
        }

        public async Task<StoreDetailsResponse> Handle(GetStoreDetailsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var storeExists = await _dbContext.Stores.Where(s => s.Uid == request.Uid).AnyAsync(cancellationToken); 

                if (!storeExists)
                {
                    throw new BadRequestException("Store doesnt exist");
                }

                var storeResponse = await _dbContext.Stores
                    .AsSplitQuery()
                    .Include(s => s.User).ThenInclude(u => u.Profile)
                    .Include(s => s.StoreSocialMedia)
                    .Where(s => s.Uid == request.Uid && !s.User.IsSuspended && s.IsActive == true)
                    .Select(s => new StoreDetailsResponse
                    {
                        Uid = s.Uid,
                        ProfileUid = s.User.Profile.Uid,
                        UserId = s.User.Id,
                        Followers = s.StoreFollowers.Count(),
                        ImageUrl = s.ImageUrl,
                        UniqueName = s.UniqueName,
                        Name = s.Name,
                        Description = s.Description,
                        StoreEmail = s.IsEmailPublic ? s.StoreEmail : null,
                        IsEmailPublic = s.IsEmailPublic,
                        LegalName = s.LegalName,
                        AffiliateId = s.Affiliate != null ? s.Affiliate.AffiliateId : null,
                        CurrencyUid = s.Currency.Uid,
                        CurrencyCode = s.Currency.Code,
                        StoreDescription = s.Description,
                        PhoneNumber = s.PhoneNumber,
                        LikesCount = s.LikesCount,
                        WebsiteUrl = s.StoreSocialMedia.WebsiteUrl,
                        FacebookUrl = s.StoreSocialMedia.FacebookUrl,
                        InstagramUrl = s.StoreSocialMedia.InstagramUrl,
                        TwitterUrl = s.StoreSocialMedia.TwitterUrl,
                        TikTokUrl = s.StoreSocialMedia.TikTokUrl,
                    }).SingleOrDefaultAsync(cancellationToken);

                var cUser = await _currentUserService.GetUserAsync();

                if (cUser != null)
                {
                    storeResponse.FollowedByMe = await _dbContext.StoreFollowers.Where(s => s.Store.Uid == storeResponse.Uid && s.FollowerId == cUser.Profile.Id).AnyAsync(cancellationToken);
                }

                return storeResponse;

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}