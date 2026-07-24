using System.Collections.Generic;
using Core.Application.Authorization;
using Xunit;

namespace XUnitTests.Orders
{
    /// <summary>
    /// Negative and positive authorization tests for viewing order details (IDOR guard).
    /// Mirrors the rule enforced in OrderService.GetOrderDetailsAsync.
    /// </summary>
    public class OrderAccessPolicyTests
    {
        private const string BuyerAUserId = "user-buyer-a";
        private const string BuyerBUserId = "user-buyer-b";
        private const string SellerUserId = "user-seller";
        private const string UnrelatedSellerUserId = "user-other-seller";

        private const int BuyerAProfileId = 1;
        private const int BuyerBProfileId = 2;

        [Fact]
        public void BuyerA_ReadingBuyerBsOrder_IsDenied()
        {
            // Order belongs to buyer B; buyer A is neither the owner nor a seller.
            var sellerIds = new List<string> { SellerUserId };

            var canView = OrderAccessPolicy.CanView(
                orderProfileId: BuyerBProfileId,
                viewerProfileId: BuyerAProfileId,
                orderItemSellerUserIds: sellerIds,
                viewerUserId: BuyerAUserId);

            Assert.False(canView);
        }

        [Fact]
        public void UnrelatedSeller_ReadingOrder_IsDenied()
        {
            // Seller of a different order has no items on this order.
            var sellerIds = new List<string> { SellerUserId };

            var canView = OrderAccessPolicy.CanView(
                orderProfileId: BuyerBProfileId,
                viewerProfileId: null,
                orderItemSellerUserIds: sellerIds,
                viewerUserId: UnrelatedSellerUserId);

            Assert.False(canView);
        }

        [Fact]
        public void LegitimateBuyer_ReadingOwnOrder_IsAllowed()
        {
            var sellerIds = new List<string> { SellerUserId };

            var canView = OrderAccessPolicy.CanView(
                orderProfileId: BuyerAProfileId,
                viewerProfileId: BuyerAProfileId,
                orderItemSellerUserIds: sellerIds,
                viewerUserId: BuyerAUserId);

            Assert.True(canView);
        }

        [Fact]
        public void LegitimateSeller_ReadingOrderWithTheirItem_IsAllowed()
        {
            // Viewer is not the buyer but sells one of the order's items.
            var sellerIds = new List<string> { SellerUserId, UnrelatedSellerUserId };

            var canView = OrderAccessPolicy.CanView(
                orderProfileId: BuyerBProfileId,
                viewerProfileId: null,
                orderItemSellerUserIds: sellerIds,
                viewerUserId: SellerUserId);

            Assert.True(canView);
        }

        [Fact]
        public void ViewerWithNoProfileAndNoSellerItems_IsDenied()
        {
            var canView = OrderAccessPolicy.CanView(
                orderProfileId: BuyerBProfileId,
                viewerProfileId: null,
                orderItemSellerUserIds: new List<string>(),
                viewerUserId: BuyerAUserId);

            Assert.False(canView);
        }
    }
}
