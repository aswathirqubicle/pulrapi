namespace Core.Infrastructure.Services.Users
{
    public class JWT
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        // public double DurationInMinutes { get; set; } = 129600; // 90 days in minutes
        public double DurationInMinutes { get; set; } = 1; // 1 minute
    }
}
