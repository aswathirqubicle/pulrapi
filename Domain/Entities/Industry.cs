using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class Industry : EntityBase
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Key { get; set; }
    }
}
