using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Search;

public class SearchRequestDto
{
    public string Term { get; set; }

    [Required]
    public string Type { get; set; } = "top";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int? PageSize { get; set; }
}