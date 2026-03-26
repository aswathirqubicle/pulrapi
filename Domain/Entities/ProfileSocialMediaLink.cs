using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Domain.Entities
{
    public class ProfileSocialMediaLink : EntityBase
    {
        public string Url { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

    }
}
