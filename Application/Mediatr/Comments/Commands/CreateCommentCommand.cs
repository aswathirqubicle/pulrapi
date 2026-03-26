using Amazon.Runtime.Internal;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Security.Validation.Attributes;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Comments.Commands
{
    public class CreateCommentCommand : IRequest<CommentResponse>
    {
        [Required(ErrorMessage = "Action is required.")]
        [EnumDataType(typeof(EntityTypeEnum))]
        public EntityTypeEnum? EntityType { get; set; }

        [SafeUid(allowNullValue:false,maxLength:50,minLength:5,ErrorMessage = "EntityUid contains invalid characters or format.")]
        public string EntityUid { get; set; }

        [SafeComment(allowNullValue:false,maxLength:1000, minLength:1,ErrorMessage = "Comment contains invalid characters or format.")]
        public string Comment { get; set; }
    }

    public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentResponse>
    {
        private readonly ILogger<CreateCommentCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly INotificationService _notificationService;

        public CreateCommentCommandHandler(ILogger<CreateCommentCommandHandler> logger, IMapper mapper,
            IApplicationDbContext dbContext, ICurrentUserService currentUserService, INotificationService notificationService)
        {
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _notificationService = notificationService;
        }

        public async Task<CommentResponse> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser.Profile == null)
                {
                    throw new BadRequestException($"User {cUser.UserName} doesnt have a profile.");
                }

                switch (request.EntityType)
                {
                    case EntityTypeEnum.POST:
                        return await CreateCommentForPost(request.EntityUid, request.Comment, cUser, cancellationToken);
                    case EntityTypeEnum.PRODUCT:
                        return await CreateCommentForProduct(request.EntityUid, request.Comment, cUser, cancellationToken);
                    default:
                        return new CommentResponse();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private async Task<CommentResponse> CreateCommentForPost(string postUid, string comment, User cUser,
            CancellationToken cancellationToken)
        {
            var post = await _dbContext.Posts.SingleOrDefaultAsync(p => p.Uid == postUid, cancellationToken);
            if (post == null)
            {
                throw new BadRequestException($"Post with uid {postUid} doesnt exist.");
            }

            var newComment = new Comment() { Post = post, CommentedBy = cUser.Profile, Content = comment };
            _dbContext.Comments.Add(newComment);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _notificationService.SaveCommentNotificationAsync(cUser.Id, postUid, newComment.Uid);

            // Get total parent comments count
            var totalCount = await _dbContext.Comments
                .Where(c => c.ParentCommentId == null && c.Post.Uid == postUid)
                .CountAsync(cancellationToken);

            // Get replies count
            var repliesCount = await _dbContext.Comments
                .Where(c => c.ParentComment.Uid == newComment.Uid && c.Post.Uid == postUid)
                .CountAsync(cancellationToken);

            var commentResponse = _mapper.Map<CommentResponse>(newComment);
            commentResponse.PostOwnerProfileId = post.User.Profile.Uid;
            commentResponse.PostUid = postUid;
            commentResponse.AuthorProfileImageUrl = newComment.CommentedBy.ImageUrl;
            commentResponse.DisplayName = newComment.CommentedBy.User.DisplayName;
            commentResponse.LikedByMe = cUser != null && cUser.Profile != null
                ? newComment.CommentLikes.Any(l => l.LikedById == cUser.Profile.Id)
                : false;
            commentResponse.TotalParentCommentsCount = totalCount;
            commentResponse.RepliesCount = repliesCount;
            commentResponse.ParentCommentUid = newComment.Uid;
            return commentResponse;
        }

        private async Task<CommentResponse> CreateCommentForProduct(string productUid, string comment, User cUser,
            CancellationToken cancellationToken)
        {
            var product = await _dbContext.Products.SingleOrDefaultAsync(p => p.Uid == productUid, cancellationToken);
            if (product == null)
            {
                throw new BadRequestException($"Product with uid {productUid} doesnt exist.");
            }

            var newComment = new Comment { Product = product, CommentedBy = cUser.Profile, Content = comment };
            _dbContext.Comments.Add(newComment);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _notificationService.SaveProductCommentNotificationAsync(cUser.Id, productUid, newComment.Uid);

            // Get total parent comments count
            var totalCount = await _dbContext.Comments
                .Where(c => c.ParentCommentId == null && c.Product.Uid == productUid)
                .CountAsync(cancellationToken);

            var commentResponse = _mapper.Map<CommentResponse>(newComment);
            commentResponse.ProductUid = productUid;
            commentResponse.AuthorProfileImageUrl = newComment.CommentedBy.ImageUrl;
            commentResponse.DisplayName = newComment.CommentedBy.User.DisplayName;
            commentResponse.LikedByMe = cUser != null && cUser.Profile != null
                ? newComment.CommentLikes.Any(l => l.LikedById == cUser.Profile.Id)
                : false;
            commentResponse.TotalParentCommentsCount = totalCount;
            return commentResponse;
        }
    }
}