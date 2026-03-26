using System.ComponentModel.DataAnnotations;
using Core.Application.Models;
using Core.Domain.Enums;

namespace Core.Application.Models
{
    public class CommentListQueryParams : PagingParamsRequest
    {
        [Required]
        public EntityTypeEnum EntityType { get; set; }
        [Required]
        public string EntityUid { get; set; }
    }
}
