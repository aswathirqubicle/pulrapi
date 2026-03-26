using Core.Application.Models.Products;

namespace Core.Application.Models.Post
{
    public class PostProductTagResponse
    {
        public double PositionLeftPercent { get; set; }
        public double PositionTopPercent { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public string ThumbnailUrl { get; set; }
        public ProductPublicResponse Product { get; set; }
    }
}
