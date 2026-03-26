namespace Core.Domain.Entities;

public class ProfileSocialMedia : EntityBase
{
    public string WebsiteUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; }
}