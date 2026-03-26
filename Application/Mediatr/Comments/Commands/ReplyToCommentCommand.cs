using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Comments.Commands
{
    public class ReplyToCommentCommand : IRequest<CommentResponse>
    {
        [SafeUid(allowNullValue:false,maxLength:50,minLength:5)]
        public string ParentCommentUid { get; set; }

        [SafeComment(allowNullValue:false,maxLength:1000,minLength:1)]
        public string Content { get; set; }
    }

    public class ReplyToCommentCommandHandler : IRequestHandler<ReplyToCommentCommand, CommentResponse>
    {
        private readonly ILogger<ReplyToCommentCommandHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public ReplyToCommentCommandHandler(
            ILogger<ReplyToCommentCommandHandler> logger,
            IMapper mapper,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _logger = logger;
            _mapper = mapper;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<CommentResponse> Handle(ReplyToCommentCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var cUser = await _currentUserService.GetUserAsync();
                if (cUser.Profile == null)
                {
                    throw new BadRequestException($"User {cUser.UserName} doesn't have a profile.");
                }

                var parentComment = await _dbContext.Comments
                    .Include(c => c.Post)
                    .Include(c => c.Product)
                    .Include(c => c.ParentComment)
                    .SingleOrDefaultAsync(c => c.Uid == request.ParentCommentUid, cancellationToken);

                if (parentComment == null)
                {
                    throw new BadRequestException($"Comment with uid {request.ParentCommentUid} doesn't exist.");
                }

                var originalParentComment = parentComment;
                while (originalParentComment.ParentComment != null)
                {
                    originalParentComment = originalParentComment.ParentComment;
                }

                var reply = new Comment
                {
                    Content = request.Content,
                    CommentedBy = cUser.Profile,
                    ParentComment = originalParentComment,
                    Post = parentComment.Post,
                    Product = parentComment.Product
                };

                _dbContext.Comments.Add(reply);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Get total parent comments count
                var totalCount = await _dbContext.Comments
                    .Where(c => c.ParentCommentId == null && 
                        ((parentComment.Post != null && c.Post.Uid == parentComment.Post.Uid) ||
                         (parentComment.Product != null && c.Product.Uid == parentComment.Product.Uid)))
                    .CountAsync(cancellationToken);

                // Get replies count for the original parent comment
                var repliesCount = await _dbContext.Comments
                    .Where(c => c.ParentCommentId == originalParentComment.Id)
                    .CountAsync(cancellationToken);

                var commentResponse = _mapper.Map<CommentResponse>(reply);
                commentResponse.PostUid = parentComment.Post?.Uid;
                commentResponse.ProductUid = parentComment.Product?.Uid;
                commentResponse.AuthorProfileImageUrl = reply.CommentedBy.ImageUrl;
                commentResponse.DisplayName = reply.CommentedBy.User.DisplayName;
                commentResponse.LikedByMe = false;
                commentResponse.TotalParentCommentsCount = totalCount;
                commentResponse.RepliesCount = repliesCount;

                return commentResponse;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
} 