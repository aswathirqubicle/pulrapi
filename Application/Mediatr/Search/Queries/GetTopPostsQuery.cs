using Application.DTOs.Search;
using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Core.Domain.Enums;

namespace Core.Application.Mediatr.Search.Queries;

public class GetTopPostsQuery : IRequest<PaginatedResultDto<PostSearchResultDto>>
{
    public string SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int? PageSize { get; set; }
}

public class GetTopPostsQueryHandler : IRequestHandler<GetTopPostsQuery, PaginatedResultDto<PostSearchResultDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private const int DefaultPageSize = 10;

    public GetTopPostsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedResultDto<PostSearchResultDto>> Handle(GetTopPostsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cUser = await _currentUserService.GetUserAsync();

            // Validate and set default values for pagination
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize ?? DefaultPageSize;
            if (pageSize <= 0) pageSize = DefaultPageSize;

            // Create the base query
            var baseQuery = _dbContext.Posts
                .Include(p => p.User)
                .Include(p => p.User.Profile)
                    .ThenInclude(pr => pr.ProfileSettings)
                .Include(p => p.MediaFile)
                .Where(p => p.IsActive &&
                (
                    (p.User.Profile.ProfileSettings == null || p.User.Profile.ProfileSettings.IsProfilePublic) ||
                    (cUser != null && cUser.Profile != null && (
                        p.User.Id == cUser.Id ||
                        _dbContext.ProfileFollowers.Any(pf =>
                            pf.ProfileId == p.User.Profile.Id && pf.FollowerId == cUser.Profile.Id)
                    ))
                ));

            // Add search term filter if provided
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                baseQuery = baseQuery.Where(p => p.IsActive &&
                (
                p.Text.ToLower().Contains(request.SearchTerm.ToLower()) ||
                p.ImgDescription.ToLower().Contains(request.SearchTerm.ToLower())
                )).OrderBy(p => p.Text);
            }

            // Get total count
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Get paginated results
            var items = await baseQuery
                .OrderByDescending(p => p.PostLikes.Count)
                .Select(p => new PostSearchResultDto
                {
                    Uid = p.Uid,
                    Caption = p.Text,
                    MediaFile = p.MediaFile != null ? new MediaFileDto
                    {
                        Url = p.MediaFile.Url,
                        FileType = p.MediaFile.MediaFileType,
                        Uid = p.MediaFile.Uid
                    } : null,
                    ThumbnailUrl = string.IsNullOrEmpty(p.ThumbnailUrl) ? (p.MediaFile != null ? (p.MediaFile.OriginalUrl ?? p.MediaFile.Url) : null) : p.ThumbnailUrl,
                    LikesCount = p.PostLikes.Count,
                    CreatedAt = p.CreatedAt,
                    Profile = new UserBasicDto
                    {
                        Uid = p.User.Profile != null ? p.User.Profile.Id : 0,
                        FullName = p.User.FirstName != null ? p.User.FirstName : string.Empty,
                        ImageUrl = p.User.Profile != null ? (p.User.Profile.ImageUrl ?? string.Empty) : string.Empty
                    }
                })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return PaginatedResultDto<PostSearchResultDto>.Create(
                page,
                pageSize,
                totalCount,
                items);
        }
        catch (Exception ex)
        {
            // Log the exception (not implemented here)
            throw new Exception("An error occurred while fetching top posts.", ex);
        }
    }
}
