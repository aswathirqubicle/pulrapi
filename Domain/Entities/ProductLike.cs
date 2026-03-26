
using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductLike : EntityBase
    {
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }
        [Required]
        public int LikedById { get; set; }
        public Profile LikedBy { get; set; }
    }
}
