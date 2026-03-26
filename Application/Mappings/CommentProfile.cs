using Core.Application.Mediatr.Comments.Queries;
using Core.Application.Models;

namespace Core.Application.Mappings
{
    public class CommentProfile : AutoMapper.Profile
    {
        public CommentProfile()
        {
            CreateMap<GetCommentsQuery, CommentListQueryParams>();

            CreateMap<PagedList<CommentResponse>, PagingResponse<CommentResponse>>().ForMember(
                            dest => dest.Items, opt => opt.MapFrom(src => src));
        }
    }
}
