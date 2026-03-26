
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class Currency : EntityBase
    {
        [Required]
        public string Code { get; set; }
        [Required]
        public string Symbol { get; set; }
        [Required]
        public string Name { get; set; }
    }
}
