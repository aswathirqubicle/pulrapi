namespace Core.Domain.Entities;

public class StoryProfileMention : EntityBase
{
    public int StoryId { get; set; }
    public Story Story { get; set; }
    public int ProfileId { get; set; }
    public Profile Profile { get; set; }
}