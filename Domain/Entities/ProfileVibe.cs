using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class ProfileVibe : EntityBase
    {
        [Required]
        public int ProfileId { get; set; }
        public Profile Profile { get; set; }
        
        [Required]
        public int VibeId { get; set; }
        public Vibe Vibe { get; set; }
    }
}
