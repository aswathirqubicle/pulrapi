
using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductMediaFile : EntityBase
    {
        public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int MediaFileId { get; set; }
        public MediaFile MediaFile { get; set; }
    }
}
