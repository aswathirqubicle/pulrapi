using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Currencies;
using Core.Application.Models.Profiles;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Profile = Core.Domain.Entities.Profile;

namespace Core.Application.Mediatr.Profiles.Queries
{
    public class GetAllUsersQuery : PagingParamsRequest, IRequest<PagingResponse<ProfileDetailsResponse>>
    {
        public new string Search { get; set; }
    }

    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, PagingResponse<ProfileDetailsResponse>>
    {
        private readonly ILogger<GetAllUsersQueryHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetAllUsersQueryHandler(ILogger<GetAllUsersQueryHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<ProfileDetailsResponse>> Handle(GetAllUsersQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                
                // Get blocked profile IDs (users who blocked me or users I blocked)
                var blockedProfileIds = new List<string>();
                if (cUser?.Profile != null)
                {
                    blockedProfileIds = await _dbContext.UserBlocks
                        .Where(ub => (ub.BlockerProfileId == cUser.Profile.Uid || ub.BlockedProfileId == cUser.Profile.Uid) && ub.IsActive)
                        .Select(ub => ub.BlockerProfileId == cUser.Profile.Uid ? ub.BlockedProfileId : ub.BlockerProfileId)
                        .ToListAsync(cancellationToken);
                }

                // Build query to get all active profiles, excluding current user and blocked users
                IQueryable<Profile> query = _dbContext.Profiles
                    .Include(p => p.User)
                        .ThenInclude(u => u.Country)
                    .Include(p => p.User)
                        .ThenInclude(u => u.Posts)
                    .Include(p => p.User)
                        .ThenInclude(u => u.Stories)
                    .Include(p => p.ProfileFollowers)
                    .Include(p => p.ProfileFollowings)
                    .Include(p => p.Gender)
                    .Include(p => p.Currency)
                    .Include(p => p.ProfileSocialMedia)
                    .Include(p => p.ProfileSocialMediaLinks)
                    .Where(p => p.IsActive && !p.User.IsSuspended);

                // Exclude current user and blocked users
                if (cUser?.Profile != null)
                {
                    query = query.Where(p => p.Uid != cUser.Profile.Uid && !blockedProfileIds.Contains(p.Uid));
                }

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var searchTerm = request.Search.ToLower().Trim();
                    query = query.Where(p => 
                        (p.User.FirstName != null && p.User.FirstName.ToLower().Contains(searchTerm)) ||
                        (p.User.LastName != null && p.User.LastName.ToLower().Contains(searchTerm)) ||
                        (p.User.UserName != null && p.User.UserName.ToLower().Contains(searchTerm)) ||
                        (p.About != null && p.About.ToLower().Contains(searchTerm)) ||
                        (p.User.CityName != null && p.User.CityName.ToLower().Contains(searchTerm)) ||
                        (p.User.Country != null && p.User.Country.Name != null && p.User.Country.Name.ToLower().Contains(searchTerm))
                    );
                }

                // Apply ordering
                if (string.IsNullOrWhiteSpace(request.Order) || string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
                else
                {
                    switch (request.OrderBy.ToLower())
                    {
                        case "username":
                            query = request.Order.ToLower() == "asc" ? query.OrderBy(p => p.User.UserName) : query.OrderByDescending(p => p.User.UserName);
                            break;
                        case "firstname":
                            query = request.Order.ToLower() == "asc" ? query.OrderBy(p => p.User.FirstName) : query.OrderByDescending(p => p.User.FirstName);
                            break;
                        case "lastname":
                            query = request.Order.ToLower() == "asc" ? query.OrderBy(p => p.User.LastName) : query.OrderByDescending(p => p.User.LastName);
                            break;
                        case "followers":
                            query = request.Order.ToLower() == "asc" ? query.OrderBy(p => p.ProfileFollowers.Count) : query.OrderByDescending(p => p.ProfileFollowers.Count);
                            break;
                        case "createdat":
                            query = request.Order.ToLower() == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt);
                            break;
                        default:
                            query = query.OrderByDescending(p => p.CreatedAt);
                            break;
                    }
                }

                // Map to response
                var queryMapped = query.Select(p => new ProfileDetailsResponse
                {
                    Uid = p.Uid,
                    Followers = p.ProfileFollowers.Count,
                    Following = p.ProfileFollowings.Count,
                    FullName = p.User.FirstName,
                    FirstName = p.User.FirstName,
                    LastName = p.User.LastName,
                    DisplayName = p.User.DisplayName,
                    Username = p.User.UserName,
                    ImageUrl = p.ImageUrl,
                    About = p.About,
                    PhoneNumber = p.User.PhoneNumber,
                    Email = p.User.Email,
                    Gender = p.Gender != null ? p.Gender.Key : null,
                    Location = p.User.Country != null ? p.User.Country.Name : p.Location,
                    UserType = p.UserType,
                    WebsiteUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.WebsiteUrl : null,
                    InstagramUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.InstagramUrl : null,
                    FacebookUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.FacebookUrl : null,
                    TwitterUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.TwitterUrl : null,
                    TikTokUrl = p.ProfileSocialMedia != null ? p.ProfileSocialMedia.TikTokUrl : null,
                    SocialMediaLinks = p.ProfileSocialMediaLinks.Select(sml => new ProfileSocialMediaLinkDto
                    {
                        Url = sml.Url,
                        Title = sml.Title,
                        Type = sml.Type
                    }).ToList(),
                    Address = p.User.Address,
                    ZipCode = p.User.ZipCode,
                    CityName = p.User.CityName,
                    CountryName = p.User.Country != null ? p.User.Country.Name : null,
                    CountryUid = p.User.Country != null ? p.User.Country.Uid : null,
                    Currency = p.Currency != null ? new CurrencyDetailsResponse
                    {
                        Uid = p.Currency.Uid,
                        Code = p.Currency.Code,
                        Symbol = p.Currency.Symbol,
                        Name = p.Currency.Name
                    } : null,
                    PostsCount = p.User.Posts != null ? p.User.Posts.Count : 0,
                    ActiveStoriesCount = p.User.Stories != null 
                        ? (cUser != null 
                            ? p.User.Stories.Count(s => s.IsActive && s.StoryExpiresIn > DateTime.UtcNow && !s.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id)) 
                            : p.User.Stories.Count(s => s.IsActive && s.StoryExpiresIn > DateTime.UtcNow)) 
                        : 0,
                    StoriesSeenCount = p.User.Stories != null 
                        ? p.User.Stories
                            .Where(s => s.IsActive && s.StoryExpiresIn > DateTime.UtcNow)
                            .SelectMany(s => s.StorySeens)
                            .Select(seen => seen.SeenById)
                            .Distinct()
                            .Count() 
                        : 0,
                    UnseenStoriesCount = p.User.Stories != null && cUser != null
                        ? p.User.Stories.Count(s => s.IsActive && s.StoryExpiresIn > DateTime.UtcNow && !s.StorySeens.Any(seen => seen.SeenById == cUser.Profile.Id))
                        : (p.User.Stories != null ? p.User.Stories.Count(s => s.IsActive && s.StoryExpiresIn > DateTime.UtcNow) : 0),
                    IsProfilePublic = true, // Default value
                    IsProfileBio = !string.IsNullOrEmpty(p.About),
                    IsInfluencer = false, // This should be calculated based on user roles if needed
                    PostedTimeAgo = p.CreatedAt, // Default to CreatedAt for now
                    CreatedAt = p.CreatedAt,
                    // Check if current user follows this profile
                    FollowedByMe = cUser != null && cUser.Profile != null ? 
                        p.ProfileFollowers.Any(pf => pf.FollowerId == cUser.Profile.Id) : false
                });

                var list = await PagedList<ProfileDetailsResponse>.ToPagedListAsync(queryMapped, request.PageNumber, request.PageSize);

                var response = _mapper.Map<PagingResponse<ProfileDetailsResponse>>(list);
                response.ItemIds = response.Items.Select(item => item.Uid).ToList();
                
                return response;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
} 