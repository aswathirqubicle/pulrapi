using System.Collections.Generic;

namespace Core.Application.Models.Products
{
    /// <summary>
    /// Response model for a variant option
    /// </summary>
    public class VariantOptionResponse
    {
        public string Uid { get; set; }
        public string OptionName { get; set; }
        public List<VariantOptionValueResponse> Values { get; set; } = new List<VariantOptionValueResponse>();
    }

    public class VariantOptionValueResponse
    {
        public string Uid { get; set; }
        public string Value { get; set; }
    }
}
