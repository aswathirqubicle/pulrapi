using System.Collections.Generic;

namespace Core.Application.Models.Profiles
{
    public class ProfileMyFollowsInfoResponse
    {
        public List<string> Followers { get; set; }
        public List<string> Following { get; set; }
        public List<string> FollowingStores { get; set; }
    }
}
