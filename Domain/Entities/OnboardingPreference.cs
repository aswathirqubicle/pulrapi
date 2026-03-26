using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class OnboardingPreference : EntityBase
    {
        [Required]
        public string Name { get; set; } 
        [Required]
        public string Key { get; set; }
        public string Description { get; set; }
        public int? GenderId { get; set; }
        public Gender Gender { get; set; } 
    }
}
