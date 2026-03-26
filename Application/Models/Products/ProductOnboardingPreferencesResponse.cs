using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Application.Models.Products
{
    public class ProductOnboardingPreferencesResponse
    {
        public string ProductUid { get; internal set; }
        public List<ProductOnboardingPreferenceResponse> Preferences { get; set; }
    }
}
