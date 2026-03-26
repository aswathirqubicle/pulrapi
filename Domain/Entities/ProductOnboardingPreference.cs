using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Domain.Entities
{
    public class ProductOnboardingPreference : EntityBase
    {
        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; }

        [Required]
        public int OnboardingPreferenceId { get; set; }
        public OnboardingPreference OnboardingPreference { get; set; }
    }
}
