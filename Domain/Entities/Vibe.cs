using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class Vibe : EntityBase
    {
        [Required]
        public string Name { get; set; }
        
        [Required]
        public string Key { get; set; }
                        
        [Required]
        public string Category { get; set; }
        
        public int DisplayOrder { get; set; }
        
    }
}
