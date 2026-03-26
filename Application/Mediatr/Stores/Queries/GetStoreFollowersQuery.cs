using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Application.Models;
using Core.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Core.Application.Mediatr.Stores.Queries
{
    public class GetStoreFollowersQuery : PagingParamsRequest, IRequest<PagingResponse<ProfileDetailsResponse>>
    {
        [Required]
        public string StoreUid { get; set; }
    }

    public class GetStoreFollowersQueryHandler : IRequestHandler<GetStoreFollowersQuery, PagingResponse<ProfileDetailsResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetStoreFollowersQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetStoreFollowersQueryHandler(IApplicationDbContext dbContext,
            ILogger<GetStoreFollowersQueryHandler> logger,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ProfileDetailsResponse>> Handle(GetStoreFollowersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                var store = await _dbContext.Stores.Where(p => p.Uid == request.StoreUid).SingleOrDefaultAsync(cancellationToken);
                if (store == null)
                {
                    throw new BadRequestException("Store doesnt exist");
                }

                IQueryable<StoreFollower> storeFollowersQueryable = _dbContext.StoreFollowers;

                var storeFollowers = storeFollowersQueryable.Where(sf => sf.StoreId == store.Id && sf.Follower.IsActive)
                    .Select(s => new ProfileDetailsResponse
                    {
                        Uid = s.Follower.Uid,
                        FullName = s.Follower.User.FirstName,
                        FirstName = s.Follower.User.FirstName,
                        LastName = s.Follower.User.LastName,
                        Username = s.Follower.User.UserName,
                        ImageUrl = s.Follower.ImageUrl,
                        Followers = s.Follower.ProfileFollowers.Count(),
                        Following = s.Follower.ProfileFollowings.Count(),
                        PostsCount = s.Follower.User.Posts.Count(),
                        About = s.Follower.About,
                        Location = s.Follower.Location,
                        WebsiteUrl = s.Follower.ProfileSocialMedia.WebsiteUrl,
                        InstagramUrl = s.Follower.ProfileSocialMedia.InstagramUrl,
                        FacebookUrl = s.Follower.ProfileSocialMedia.FacebookUrl,
                        TwitterUrl = s.Follower.ProfileSocialMedia.TwitterUrl,
                        TikTokUrl = s.Follower.ProfileSocialMedia.TikTokUrl,
                        ActiveStoriesCount = s.Follower.User.Stories.Count,
                        FollowedByMe = cUser != null ? storeFollowersQueryable
                        .Where(sf => sf.Follower.Uid == cUser.Profile.Uid && sf.Store.Uid == s.Store.Uid).Any() : false,
                        Stores = s.Follower.User.Stores.Select(s => new StoreDetailsResponse
                        {
                            Followers = s.StoreFollowers.Count(),
                            Name = s.Name,
                            ImageUrl = s.ImageUrl,
                            Uid = s.Uid,
                            UniqueName = s.UniqueName
                        }).ToList()
                    });

                var list = await PagedList<ProfileDetailsResponse>.ToPagedListAsync(storeFollowers, request.PageNumber, request.PageSize);
                return _mapper.Map<PagingResponse<ProfileDetailsResponse>>(list);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
