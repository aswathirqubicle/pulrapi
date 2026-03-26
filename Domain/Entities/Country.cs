
using System.ComponentModel.DataAnnotations;
using Core.Domain.Entities;

namespace Core.Domain.Entities
{
    public class Country : EntityBase
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Iso2 { get; set; }
        [Required]
        public string Iso3 { get; set; }
        public string Iso4 { get; set; }
    }
}
