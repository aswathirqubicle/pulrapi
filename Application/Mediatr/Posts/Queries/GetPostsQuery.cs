using AutoMapper;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Application.Models.Post;
using Core.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Posts.Queries
{
    public class GetPostsQuery : PagingParamsRequest, IRequest<PagingResponse<PostResponse>>
    {
        public string EntityUid { get; set; }
        public ProfileTypeEnum ProfileType { get; set; } = ProfileTypeEnum.All;
        public PostTypeEnum PostType { get; set; } = PostTypeEnum.All;
        public PostSortingLogicEnum SortingLogic { get; set; } = PostSortingLogicEnum.Newest;
        public virtual ICollection<PostProductTagResponse> PostProductTags { get; set; }
        public string CurrencyCode { get; set; } = "AED";
        public string Categories { get; set; }
        public string Tags { get; set; }
        public string Hashtag { get; set; }
        public ProductTypeEnum? ProductType { get; set; }
    }

    public class GetPostsQueryHandler : IRequestHandler<GetPostsQuery, PagingResponse<PostResponse>>
    {
        private readonly ILogger<GetPostsQueryHandler> _logger;
        private readonly IPostService _postService;
        private readonly IMapper _mapper;

        public GetPostsQueryHandler(ILogger<GetPostsQueryHandler> logger, IPostService postService, IMapper mapper)
        {
            _logger = logger;
            _postService = postService;
            _mapper = mapper;
        }

        public async Task<PagingResponse<PostResponse>> Handle(GetPostsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var queryParams = _mapper.Map<GetPostsQueryParams>(request);

                if (queryParams.PostType == PostTypeEnum.MyStyle)
                {
                    var postsMyStylePaged = await _postService.GetPostsMyStyle(queryParams);
                    postsMyStylePaged.ItemIds = postsMyStylePaged.Items.Select(item => item.Uid).ToList();
                    return postsMyStylePaged;
                }

                var postsPaged = await _postService.GetPosts(queryParams);
                postsPaged.ItemIds = postsPaged.Items.Select(item => item.Uid).ToList();
                return postsPaged;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
