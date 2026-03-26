
using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class StoreRating : EntityBase
    {
        [Required]
        public int StoreId { get; set; }
        public Store Store { get; set; }
        [Required]
        public int RatedById { get; set; }
        public Profile RatedBy { get; set; }
        [Required]
        [Range(0.5, 5)]
        public double NumberOfStars { get; set; }
    }
}
