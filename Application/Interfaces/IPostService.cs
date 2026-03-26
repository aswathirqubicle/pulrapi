using System.Threading.Tasks;
using Core.Application.Models;
using Core.Application.Models.Post;

namespace Core.Application.Interfaces
{
    public interface IPostService
    {
        Task<PagingResponse<PostResponse>> GetPosts(GetPostsQueryParams queryParams);
        Task<ToggleLikePostDto> PostToggleLike(string postUid);
        Task ToggleToMyStyle(string postUid);
        Task<PagingResponse<PostResponse>> GetPostsMyStyle(GetPostsQueryParams queryParams);
    }
}
