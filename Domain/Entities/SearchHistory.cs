using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class SearchHistory : EntityBase
{
    public string Term { get; set; }
    public int SearchCount { get; set; }
    public string UserId { get; set; }
    public SearchHistoryType Type { get; set; }
    public User User { get; set; }
}