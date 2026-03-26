namespace Core.Application.Models.Users
{
    public class DeviceDto
    {
        public string Brand { get; set; }
        public string ModelName { get; set; }
        public string OsVersion { get; set; }
        public string AppVersion { get; set; }
        public string DeviceIdentifier { get; set; }
        public string PushToken { get; set; }
    }
} 