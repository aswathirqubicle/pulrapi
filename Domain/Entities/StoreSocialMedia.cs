namespace Core.Domain.Entities;

public class StoreSocialMedia : EntityBase
{
    public string WebsiteUrl { get; set; }
    public string FacebookUrl { get; set; }
    public string InstagramUrl { get; set; }
    public string TwitterUrl { get; set; }
    public string TikTokUrl { get; set; }
    public int StoreId { get; set; }
    public Store Store { get; set; }
}