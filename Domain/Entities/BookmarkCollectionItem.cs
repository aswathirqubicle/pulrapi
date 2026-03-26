namespace Core.Domain.Entities
{
    public class BookmarkCollectionItem : EntityBase
    {
        public new int Id { get; set; }
        public int PostId { get; set; }
        public Post Post { get; set; }
        public int BookmarkCollectionId { get; set; }
        public BookmarkCollection BookmarkCollection { get; set; }
    }
} 