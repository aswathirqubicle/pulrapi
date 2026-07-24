
using System;

namespace Core.Application.Constants
{
    public static class AwsLocationNames
    {
        public const string S3UploadBucket = "Aws:S3UploadBucket";
        public const string S3DocumentsBucket = "Aws:S3DocumentsBucket";
        public const string PublicUploadFolder = "Aws:PublicUploadFolder";
        public const string LogoFileName = "Aws:LogoFileName";
        public const string AwsRegion = "Aws:AwsRegion";
        [Obsolete("Use AwsRegion instead. This will be removed in a future version.")]
        public const string CloudwatchRegion = "Aws:CloudwatchRegion";
    }
}
