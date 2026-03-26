namespace Core.Domain.Entities;

public class StoryHashTag : EntityBase
{
    public int StoryId { get; set; }
    public Story Story { get; set; }
    public int HashTagId { get; set; }
    public Hashtag Hashtag { get; set; }
}