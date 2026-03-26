using System;
using System.Collections.Generic;

namespace Core.Domain.Entities
{
    public class BookmarkCollection : EntityBase
    {
        public new int Id { get; set; }
        public string Name { get; set; }
        public int ProfileId { get; set; }
        public string ProfileUid { get; set; }
        public Profile Profile { get; set; }
        //public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
        public ICollection<BookmarkCollectionItem> BookmarkCollectionItems { get; set; }
    }
} 