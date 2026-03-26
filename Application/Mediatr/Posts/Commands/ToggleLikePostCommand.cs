using Core.Application.Interfaces;
using Core.Application.Models.Post;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Posts.Commands
{
    public class ToggleLikePostCommand : IRequest<ToggleLikePostResponse>
    {
        [Required]
        public string PostUid { get; set; }
    }

    public class ToggleLikePostCommandHandler : IRequestHandler<ToggleLikePostCommand, ToggleLikePostResponse>
    {
        private readonly ILogger<ToggleLikePostCommandHandler> _logger;
        private readonly IPostService _postService;

        public ToggleLikePostCommandHandler(ILogger<ToggleLikePostCommandHandler> logger, IPostService postService)
        {
            _logger = logger;
            _postService = postService;
        }
        public async Task<ToggleLikePostResponse> Handle(ToggleLikePostCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var res = await _postService.PostToggleLike(request.PostUid);
                return new ToggleLikePostResponse()
                {
                    LikedByMe = res.LikedByMe,
                    LikesCount = res.LikesCount
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }


}
