using Amazon;

namespace Core.Application.Helpers
{
    /// <summary>
    /// Helper class to convert AWS region string to RegionEndpoint
    /// </summary>
    public static class AwsRegionHelper
    {
        /// <summary>
        /// Converts region name string to Amazon RegionEndpoint
        /// </summary>
        /// <param name="regionName">Region name (e.g., "ap-south-1", "me-south-1")</param>
        /// <returns>RegionEndpoint for the specified region</returns>
        public static RegionEndpoint GetRegionEndpoint(string regionName)
        {
            if (string.IsNullOrWhiteSpace(regionName))
            {
                // Default fallback to ap-south-1
                return RegionEndpoint.APSouth1;
            }

            return regionName.ToLower() switch
            {
                "ap-south-1" => RegionEndpoint.APSouth1,
                "ap-southeast-1" => RegionEndpoint.APSoutheast1,
                "ap-southeast-2" => RegionEndpoint.APSoutheast2,
                "ap-northeast-1" => RegionEndpoint.APNortheast1,
                "ap-northeast-2" => RegionEndpoint.APNortheast2,
                "me-south-1" => RegionEndpoint.MESouth1,
                "eu-west-1" => RegionEndpoint.EUWest1,
                "eu-west-2" => RegionEndpoint.EUWest2,
                "eu-central-1" => RegionEndpoint.EUCentral1,
                "us-east-1" => RegionEndpoint.USEast1,
                "us-east-2" => RegionEndpoint.USEast2,
                "us-west-1" => RegionEndpoint.USWest1,
                "us-west-2" => RegionEndpoint.USWest2,
                "sa-east-1" => RegionEndpoint.SAEast1,
                "ca-central-1" => RegionEndpoint.CACentral1,
                _ => RegionEndpoint.APSouth1 // Default fallback
            };
        }
    }
}