
namespace Core.Application.Constants
{
    public static class PulrGlobalConfig
    {
        public static class PostImage {
            public const int Width = 800;
            public const int Height = 800;
        }

        public static class AvatarImage {
            public const int Width = 200;
            public const int Height = 200;
        }

        public static class BannerImage
        {
            public const int Width =  800;
            public const int Height = 300;
        }

        public static class ProductImage
        {
            public const int Width = 800;
            public const int Height = 800;
        }

        public static class MediaFile
        {
            public const decimal MaxSizeMB = 10;
        }

        public static class OrderSettings
        {
            public const int DeliveryExtensionHours = 72;
            public const int RefundWindowDays = 3;
            public const int ExchangeWindowDays = 21;
            public const int EscrowHoldDays = 21;
            public const decimal DefaultCommissionRate = 0.01m;
            public const decimal DefaultVatRate = 0.05m;
            public const decimal DefaultPlatformFeePercentage = 0.25m;
            public const decimal DefaultDirectSaleSellerPercentage = 0.75m;
            public const decimal DefaultCollabSaleSellerPercentage = 0.65m;
            public const decimal DefaultCollabSaleCreatorPercentage = 0.10m;
            public const decimal DefaultMinimumWithdrawalAmount = 50.00m;
            public const decimal DefaultStripeFeePercentage = 0.039m;
            public const decimal DefaultStripeFixedFee = 1.00m;
        }
    }
}
