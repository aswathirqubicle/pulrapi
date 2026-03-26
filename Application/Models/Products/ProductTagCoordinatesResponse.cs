namespace Core.Application.Models.Products
{
    public class ProductTagCoordinatesResponse
    {
        public double PositionLeftPercent { get; internal set; }
        public double PositionTopPercent { get; internal set; }
        public ProductDetailsResponse Product { get; internal set; }
    }
}
