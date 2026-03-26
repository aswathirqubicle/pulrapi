
using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class ProductPair : EntityBase
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int PairId { get; set; }
        public Product Pair { get; set; }
    }
}
