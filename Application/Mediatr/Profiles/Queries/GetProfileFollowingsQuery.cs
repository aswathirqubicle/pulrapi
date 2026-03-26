using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Profiles;
using Core.Application.Models.Stores;
using Core.Domain.Entities;
using Core.Domain.Views;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for List<string>

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetProfileFollowingsQuery : PagingParamsRequest, IRequest<PagingResponse<ProfileDetailsResponse>>
    {
        [Required]
        public string ProfileUid { get; set; }
    }

    public class GetProfileFollowingsQueryHandler : IRequestHandler<GetProfileFollowingsQuery, PagingResponse<ProfileDetailsResponse>>
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<GetProfileFollowingsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetProfileFollowingsQueryHandler(IApplicationDbContext dbContext, 
            ILogger<GetProfileFollowingsQueryHandler> logger, 
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ProfileDetailsResponse>> Handle(GetProfileFollowingsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();

                var profile = await _dbContext.Profiles
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.Uid == request.ProfileUid, cancellationToken);
                if (profile == null)
                {
                    profile = await _dbContext.Profiles
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.User.UserName.ToLower() == request.ProfileUid.ToLower(), cancellationToken);
                }
                if (profile == null)
                {
                    throw new BadRequestException("Profile doesn't exist");
                }

                // 1. Get followed Profile IDs
                var followedProfileIds = await _dbContext.ProfileFollowers
                    .AsNoTracking()
                    .Where(pf => pf.FollowerId == profile.Id)
                    .Select(pf => pf.ProfileId)
                    .ToListAsync(cancellationToken);

                // 2. Fetch profiles, including user navigation
                var followingProfiles = await _dbContext.Profiles
                    .Where(p => followedProfileIds.Contains(p.Id))
                    .Include(p => p.User)
                    .Include(p => p.ProfileFollowers)
                    .Include(p => p.ProfileFollowings)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // 3. Project each profile, manually ensuring visibility of all fields; fallback for debug if User is null
                var mappedProfiles = followingProfiles.Select(p => new ProfileDetailsResponse
                {
                    FirstName    = p.User?.FirstName    ?? "NO_USER",
                    LastName     = p.User?.LastName     ?? "NO_USER",
                    DisplayName  = p.User?.DisplayName  ?? "NO_USER",
                    Username     = p.User?.UserName     ?? "NO_USER",
                    FullName     = p.User?.FirstName ?? "NO_USER",
                    Uid          = p.Uid,
                    ImageUrl     = p.ImageUrl,
                    About        = p.About,
                    PhoneNumber  = p.User?.PhoneNumber,
                    IsProfilePublic = false,
                    Gender       = p.Gender?.Key,
                    Location     = p.User?.Country != null ? p.User.Country.Name : p.Location,
                    UserType     = p.UserType,
                    WebsiteUrl   = p.ProfileSocialMedia?.WebsiteUrl,
                    InstagramUrl = p.ProfileSocialMedia?.InstagramUrl,
                    FacebookUrl  = p.ProfileSocialMedia?.FacebookUrl,
                    TwitterUrl   = p.ProfileSocialMedia?.TwitterUrl,
                    TikTokUrl    = p.ProfileSocialMedia?.TikTokUrl,
                    SocialMediaLinks = p.ProfileSocialMediaLinks != null ? p.ProfileSocialMediaLinks.Select(sml => new ProfileSocialMediaLinkDto { /* map as needed */ }).ToList() : new List<ProfileSocialMediaLinkDto>(),
                    Email            = p.User?.Email,
                    Followers        = p.ProfileFollowers?.Count ?? 0,
                    Following        = p.ProfileFollowings?.Count ?? 0,
                    IsStore          = false,
                    CreatedAt        = p.CreatedAt,
                    // Copy any extra fields you need
                });

                // 4. Page in-memory
                var combined = mappedProfiles.OrderByDescending(x => x.CreatedAt).ToList();
                var pagedList = PagedList<ProfileDetailsResponse>.ToPagedList(combined, request.PageNumber, request.PageSize);
                return _mapper.Map<PagingResponse<ProfileDetailsResponse>>(pagedList);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
