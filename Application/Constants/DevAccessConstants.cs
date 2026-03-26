namespace Core.Application.Constants
{
    /// <summary>
    /// Configuration key names for development-only access control.
    /// </summary>
    public static class DevAccessConstants
    {
        /// <summary>
        /// Config key for the dev-tool security passcode (appsettings.development.json → DevAccess:Passcode).
        /// </summary>
        public const string PasscodeConfigKey = "DevAccess:Passcode";
    }
}
