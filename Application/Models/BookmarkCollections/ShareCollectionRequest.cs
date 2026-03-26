using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Models.BookmarkCollections
{
    public class ShareCollectionRequest
    {
        public string CollectionUid { get; set; }
        public string TargetProfileUid { get; set; }
    }
}
