
using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class StoreIndustry
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int StoreId { get; set; }
        public Store Store { get; set; }
        [Required]
        public int IndustryId { get; set; }
        public Industry Industry { get; set; }
    }
}
