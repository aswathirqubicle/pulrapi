using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Models.Profiles;
using Core.Domain.Entities;

namespace Core.Application.Interfaces
{
    public interface IProfileService
    {
        Task<(string username, string uid)> ProfileToggleFollow(string profileUid);
        Task<(string username, string uid)> AcceptFollowRequest(string requesterProfileUid);
        Task<(string username, string uid)> RejectFollowRequest(string requesterProfileUid);
        Task<(string username, string uid)> ToggleFollowRequest(string targetProfileUid);
        Task Create(User user, Domain.Enums.GenderEnum? gender = null, string userType = null);
        Task<string> ProfileUpdateAvatarImage(Profile profile, IFormFile? image);
        Task<MyProfileDetailsResponse> GetMy();
        Task<string> Update(ProfileUpdateDto profileUpdateDto);
        Task<List<ProfileResponse>> MapProfileResponseList(IQueryable<Profile> profiles, CancellationToken ct);
        Task<List<string>> SearchHandles(string search);
        Task<User> GetCurrentUserWithProfile();
    }
}
