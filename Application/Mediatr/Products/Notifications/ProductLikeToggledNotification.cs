using MediatR;

namespace Core.Application.Mediatr.Products.Notifications
{
    public class ProductLikeToggledNotification : INotification
    {
        public string ProductUid { get; set; }
    }
}
