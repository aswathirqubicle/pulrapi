
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
            /// <summary>
            /// Number of hours buyer can extend delivery countdown for shipped items.
            /// Can only be extended once per item.
            /// </summary>
            public const int DeliveryExtensionHours = 72;
        }
    }
}
