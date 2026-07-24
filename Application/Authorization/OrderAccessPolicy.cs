using System.Collections.Generic;
using System.Linq;

namespace Core.Application.Authorization
{
    /// <summary>
    /// Central authorization rule for viewing a single order's details.
    /// A viewer may see an order only if they are the buyer, or a seller of at
    /// least one item on that order. Any other authenticated user must be denied
    /// to prevent IDOR access to buyer/shipping/billing/payment data.
    /// </summary>
    public static class OrderAccessPolicy
    {
        /// <param name="orderProfileId">The buyer profile id that owns the order.</param>
        /// <param name="viewerProfileId">The requesting user's profile id (null if they have no profile).</param>
        /// <param name="orderItemSellerUserIds">User ids of the sellers for the order's items.</param>
        /// <param name="viewerUserId">The requesting user's id.</param>
        public static bool CanView(
            int orderProfileId,
            int? viewerProfileId,
            IEnumerable<string> orderItemSellerUserIds,
            string viewerUserId)
        {
            bool isBuyer = viewerProfileId.HasValue && orderProfileId == viewerProfileId.Value;

            bool isSellerForAnyOrderItem = !string.IsNullOrEmpty(viewerUserId)
                && orderItemSellerUserIds != null
                && orderItemSellerUserIds.Any(id => id == viewerUserId);

            return isBuyer || isSellerForAnyOrderItem;
        }
    }
}
