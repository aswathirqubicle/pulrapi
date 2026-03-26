using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Application.Models.BookmarkCollections;
using Core.Application.Models.Post;

namespace Core.Application.Interfaces
{
    public interface IBookmarkCollectionService
    {
        Task<BookmarkCollectionResponse> CreateCollectionAsync(string name, string profileId, string postId = null);
        Task<BookmarkCollectionResponse> UpdateCollectionAsync(string Uid, string name, string profileId);
        Task DeleteCollectionAsync(string Uid, string profileId);
        Task AddPostToCollectionAsync(string postId, string collectionUid, string profileId);
        Task RemovePostFromCollectionAsync(string postId, string collectionUid, string profileId);
        Task<List<BookmarkCollectionResponse>> SearchCollectionsAsync(string searchTerm);
        Task<BookmarkCollectionResponse> GetCollectionByUidAsync(string Uid);
        Task ShareCollectionWithUserAsync(string collectionUid, string senderProfileUid, string targetProfileUid);
        Task<List<BookmarkCollectionResponse>> GetAllCollectionsWithPostsAsync(string profileId);
        Task<PostResponse> GetPostByUidAsync(string postUid);
    }
} 