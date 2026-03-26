using System;
using Core.Domain.Common;

namespace Core.Domain.Entities
{
    public class FollowRequest : EntityBase
    {
        public string RequesterProfileId { get; set; }
        public string TargetProfileId { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}