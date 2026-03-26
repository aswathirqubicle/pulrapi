namespace Core.Application.Helpers
{
    /// <summary>
    /// Helper class for email logo HTML generation.
    /// Follows Single Responsibility Principle - only handles HTML tag generation.
    /// Logo conversion and attachment creation are handled by EmailLogoService.
    /// </summary>
    public static class EmailLogoHelper
    {
        /// <summary>
        /// Gets the HTML img tag for the logo using CID embedding.
        /// Use this in email templates instead of external URLs.
        /// </summary>
        /// <param name="width">Width in pixels (default: 120)</param>
        /// <param name="height">Height in pixels (default: 40, based on SVG aspect ratio)</param>
        /// <param name="additionalStyles">Additional CSS styles to apply</param>
        /// <returns>HTML img tag with cid:pulr-logo-id source</returns>
        public static string GetLogoImgTag(int width = 120, int height = 40, string additionalStyles = "")
        {
            var style = $"width: {width}px; height: {height}px; margin-bottom: 30px; display: block; border: 0; {additionalStyles}".TrimEnd();
            return $@"<img src=""cid:pulr-logo-id@pulr.co"" alt=""PULR"" width=""{width}"" height=""{height}"" style=""{style}"" />";
        }
    }
}
