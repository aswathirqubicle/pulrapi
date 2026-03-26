using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class Hashtag : EntityBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public new int Id { get; set; }
        [Required]
        public string Value { get; set; }

        public virtual ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
        public virtual ICollection<StoryHashTag> SpotHashTags { get; set; } = new List<StoryHashTag>();
    }
}
