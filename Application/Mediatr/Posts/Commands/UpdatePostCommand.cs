using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Core.Application.Exceptions;
using Core.Application.Interfaces;
using Core.Application.Mediatr.Posts.Queries;
using Core.Application.Models.Post;
using Core.Application.Security.Validation.Attributes;
using Core.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class UpdatePostCommand : IRequest<PostDetailsResponse>
    {
        [SafeUid(allowNullValue: false, maxLength: 50, minLength: 1, validateGuidFormat: true)]
        public string PostUid { get; set; }

        // Editable fields
        public string Text { get; set; }
        public string Location { get; set; }

        // Usernames to tag as people; replaces existing people tags
        public List<string> Mentions { get; set; } = new List<string>();

        // Optional: replace product tags along with other updates
        public List<PostProductTagDto> PostProductTags { get; set; }
    }

    public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, PostDetailsResponse>
    {
        private readonly ILogger<UpdatePostCommandHandler> _logger;
        private readonly IApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public UpdatePostCommandHandler(
            ILogger<UpdatePostCommandHandler> logger,
            IApplicationDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper,
            IMediator mediator)
        {
            _logger = logger;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _mediator = mediator;
        }

        public async Task<PostDetailsResponse> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var currentUser = await _currentUserService.GetUserAsync(true);

                var post = await _dbContext.Posts
                    .Include(p => p.User)
                    .Include(p => p.PostProfileMentions)
                        .ThenInclude(ppm => ppm.Profile)
                            .ThenInclude(pr => pr.User)
                    .SingleOrDefaultAsync(p => p.Uid == request.PostUid && p.IsActive, cancellationToken);

                if (post == null)
                {
                    throw new NotFoundException($"Post with uid {request.PostUid} not found");
                }

                if (post.User.Id != currentUser.Id)
                {
                    throw new ForbiddenException("You can only edit your own posts");
                }

                // Update caption and location (only those are editable)
                if (request.Text != null)
                {
                    post.Text = request.Text;
                }
                if (request.Location != null)
                {
                    post.Location = request.Location;
                }

                // Replace tagged people (mentions) based on usernames
                if (request.Mentions != null)
                {
                    var normalizedRequestedUsernames = request.Mentions
                        .Where(u => !string.IsNullOrWhiteSpace(u))
                        .Select(u => u.Trim().StartsWith("@") ? u.Trim().Substring(1) : u.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Load profiles for requested usernames
                    var profilesToTag = await _dbContext.Profiles
                        .Include(pr => pr.User)
                        .Where(pr => normalizedRequestedUsernames.Contains(pr.User.UserName))
                        .ToListAsync(cancellationToken);

                    // Remove existing mentions
                    if (post.PostProfileMentions.Any())
                    {
                        _dbContext.PostProfileMentions.RemoveRange(post.PostProfileMentions);
                        post.PostProfileMentions.Clear();
                    }

                    // Add new mentions
                    foreach (var profile in profilesToTag)
                    {
                        post.PostProfileMentions.Add(new PostProfileMention { Profile = profile });
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                // If product tags are provided, replace them using existing command/handler
                if (request.PostProductTags != null)
                {
                    await _mediator.Send(new ReplacePostProductTagsCommand
                    {
                        PostUid = request.PostUid,
                        PostProductTags = request.PostProductTags
                    }, cancellationToken);
                }

                // Return fresh details
                return await _mediator.Send(new GetPostQuery { Uid = post.Uid }, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}


