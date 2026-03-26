namespace Core.Application.Models.Users
{
    public class CreateNotificationSettingsRequest
    {
        public string DeviceId { get; set; }
        public string PushToken { get; set; }
    }
} 