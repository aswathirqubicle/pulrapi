using System.Collections.Generic;
using System.Linq;
using Core.Domain.Entities;

namespace Core.Application.Models.Orders
{
    public class PaymentBreakdownResponse
    {
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal? Vat { get; set; }          // null in seller view
        public decimal Total { get; set; }
        public List<SellerPaymentBreakdown> Sellers { get; set; }

        public static PaymentBreakdownResponse Build(
            decimal? vatAmount,
            IEnumerable<OrderProductAffiliate> affiliates,
            bool isBuyer)
        {
            var items = affiliates?
                .Where(a => a.ProductPriceSnapshot.HasValue)
                .ToList();

            if (items == null || !items.Any()) return null;

            var sellerGroups = items
                .GroupBy(a => a.ProfileUidSnapshot ?? a.ProfileUsernameSnapshot ?? "unknown")
                .ToList();

            decimal totalSubtotal = items.Sum(a => (a.ProductPriceSnapshot ?? 0m) * a.ProductQuantity);
            decimal totalShipping = items.Sum(a => a.ShippingCostSnapshot ?? 0m);
            decimal vat = isBuyer ? (vatAmount ?? 0m) : 0m;

            decimal buyerTotal = totalSubtotal + totalShipping + vat;

            var sellers = new List<SellerPaymentBreakdown>();
            decimal assignedVat = 0m;
            decimal netBase = totalSubtotal + totalShipping;

            for (int i = 0; i < sellerGroups.Count; i++)
            {
                var group = sellerGroups[i];
                var groupItems = group.ToList();

                decimal sellerSubtotal = groupItems.Sum(a => (a.ProductPriceSnapshot ?? 0m) * a.ProductQuantity);
                decimal sellerShipping = groupItems.Sum(a => a.ShippingCostSnapshot ?? 0m);

                decimal sellerVatShare;
                if (i == sellerGroups.Count - 1)
                {
                    sellerVatShare = vat - assignedVat;
                }
                else
                {
                    sellerVatShare = netBase > 0
                        ? System.Math.Round(vat * ((sellerSubtotal + sellerShipping) / netBase), 2)
                        : 0m;
                    assignedVat += sellerVatShare;
                }

                decimal sellerTotal = isBuyer
                    ? sellerSubtotal + sellerShipping + sellerVatShare
                    : sellerSubtotal + sellerShipping;

                sellers.Add(new SellerPaymentBreakdown
                {
                    SellerName = groupItems[0].ProfileUsernameSnapshot ?? "Unknown Seller",
                    SellerProfileUid = groupItems[0].ProfileUidSnapshot,
                    Subtotal = isBuyer ? (decimal?)null : sellerSubtotal,
                    Shipping = sellerShipping,
                    Total = sellerTotal,
                    Items = groupItems.Select(a => new OrderItemBreakdown
                    {
                        ProductUid = a.Product?.Uid,
                        ProductName = a.ProductNameSnapshot,
                        ImageUrl = a.PrimaryImageUrlSnapshot,
                        UnitPrice = a.ProductPriceSnapshot ?? 0m,
                        Quantity = a.ProductQuantity,
                        Shipping = a.ShippingCostSnapshot ?? 0m
                    }).ToList()
                });
            }

            return new PaymentBreakdownResponse
            {
                Subtotal = totalSubtotal,
                Shipping = totalShipping,
                Vat = isBuyer ? vat : (decimal?)null,
                Total = isBuyer ? buyerTotal : totalSubtotal + totalShipping,
                Sellers = sellers
            };
        }
    }
}
