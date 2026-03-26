using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Domain.Entities
{
    public class StorySeen : EntityBase
    {
        public new int Id { get; set; } // Primary key
        public int StoryId { get; set; } // Foreign key to the Story
        public Story Story { get; set; } // Navigation property
        public int SeenById { get; set; } // Foreign key to the Profile (user who saw the story)
        public Profile SeenBy { get; set; } // Navigation property
        public DateTime SeenAt { get; set; } // Timestamp when the story was seen
    }
}
