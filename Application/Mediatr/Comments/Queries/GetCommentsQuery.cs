using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Comments.Queries;
using Core.Application.Models;
using Core.Domain.Entities;
using Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Core.Application.Security.Validation.Attributes;

namespace Core.Application.Mediatr.Comments.Queries
{
    public class GetCommentsQuery : PagingParamsRequest, IRequest<PagingResponse<CommentResponse>>
    {
        [Required]
        [EnumDataType(typeof(EntityTypeEnum))]
        public EntityTypeEnum EntityType { get; set; }

        [SafeUid(allowNullValue:false,maxLength:50,minLength:5,ErrorMessage = "EntityUid contains invalid characters or format.")]
        public string EntityUid { get; set; }
    }

    public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, PagingResponse<CommentResponse>>
    {
        private readonly ILogger<GetCommentsQueryHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IApplicationDbContext _dbContext;

        public GetCommentsQueryHandler(ILogger<GetCommentsQueryHandler> logger, ICurrentUserService currentUserService, IMapper mapper, IApplicationDbContext dbContext)
        {
            _logger = logger;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _dbContext = dbContext;
        }

        public async Task<PagingResponse<CommentResponse>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = _mapper.Map<CommentListQueryParams>(request);
                var currentUser = await _currentUserService.GetUserAsync();
                
                // Optimized: Single query to get both counts and main data
                var baseQuery = _dbContext.Comments
                    .Where(c => c.IsActive &&
                        ((request.EntityType == EntityTypeEnum.POST && c.Post.Uid == request.EntityUid) ||
                         (request.EntityType == EntityTypeEnum.PRODUCT && c.Product.Uid == request.EntityUid)));

                // Get counts sequentially to avoid DbContext threading issues
                var totalCount = await baseQuery
                    .Where(c => c.ParentCommentId == null)
                    .CountAsync(cancellationToken);

                var totalCommentCount = await baseQuery
                    .CountAsync(cancellationToken);

                // Build optimized query with selective includes
                IQueryable<Comment> query = _dbContext.Comments
                    .Include(q => q.CommentedBy)
                        .ThenInclude(p => p.User)
                    .Include(q => q.CommentLikes)
                    .Include(q => q.Replies.Where(r => r.IsActive))
                        .ThenInclude(r => r.CommentedBy)
                            .ThenInclude(p => p.User)
                    .Include(q => q.Replies.Where(r => r.IsActive))
                        .ThenInclude(r => r.CommentLikes);

                if (queryParams.EntityType == EntityTypeEnum.POST)
                {
                    query = query.Where(e => e.IsActive && e.Post.Uid == queryParams.EntityUid && e.ParentCommentId == null);
                }
                else if (queryParams.EntityType == EntityTypeEnum.PRODUCT)
                {
                    query = query.Where(e => e.IsActive && e.Product.Uid == queryParams.EntityUid && e.ParentCommentId == null);
                }

                // Optimize by caching current user profile ID
                var currentUserProfileId = currentUser?.Profile?.Id;
                var queryMapped = query.Select(e => new CommentResponse
                {
                    PostUid = request.EntityType == EntityTypeEnum.POST ? request.EntityUid : null,
                    PostOwnerProfileId = e.Post != null ? e.Post.User.Profile.Uid : null,
                    ProductUid = request.EntityType == EntityTypeEnum.PRODUCT ? request.EntityUid : null,
                    AuthorProfileImageUrl = e.CommentedBy.ImageUrl,
                    DisplayName = e.CommentedBy.User.DisplayName,
                    CreatedAt = e.CreatedAt,
                    CommentedBy = e.CommentedBy.User.UserName,
                    CommentedByUid = e.CommentedBy.Uid,
                    Content = e.Content,
                    Uid = e.Uid,
                    LikedByMe = currentUserProfileId != null && e.CommentLikes.Any(l => l.LikedById == currentUserProfileId),
                    LikesCount = e.CommentLikes.Count,
                    RepliesCount = e.Replies.Count(r => r.IsActive),
                    TotalParentCommentsCount = totalCount,
                    Replies = e.Replies.Where(r => r.IsActive).Select(r => new CommentResponse
                    {
                        PostUid = request.EntityType == EntityTypeEnum.POST ? request.EntityUid : null,
                        ProductUid = request.EntityType == EntityTypeEnum.PRODUCT ? request.EntityUid : null,
                        AuthorProfileImageUrl = r.CommentedBy.ImageUrl,
                        DisplayName = r.CommentedBy.User.DisplayName,
                        CreatedAt = r.CreatedAt,
                        CommentedBy = r.CommentedBy.User.UserName,
                        CommentedByUid = r.CommentedBy.Uid,
                        Content = r.Content,
                        Uid = r.Uid,
                        ParentCommentUid = e.Uid,
                        LikedByMe = currentUserProfileId != null && r.CommentLikes.Any(l => l.LikedById == currentUserProfileId),
                        LikesCount = r.CommentLikes.Count,
                        RepliesCount = r.Replies.Count,
                        TotalParentCommentsCount = totalCount,
                    }).ToList()
                });

                var list = await PagedList<CommentResponse>.ToPagedListAsync(queryMapped, queryParams.PageNumber, queryParams.PageSize, totalCommentCount);

                var res = _mapper.Map<PagingResponse<CommentResponse>>(list);

                // res.ItemIds = res.Items.Select(item => item.Uid).ToList();

                return res;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}