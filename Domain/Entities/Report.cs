using System;
using System.ComponentModel.DataAnnotations;
using Core.Domain.Enums;

namespace Core.Domain.Entities
{
    public class Report : EntityBase
    {
        [Required]
        public string EntityUid { get; set; }
        
        [Required]
        public ReportTypeEnum ReportType { get; set; }
        
        [Required]
        public string ReportedById { get; set; }
        public User ReportedBy { get; set; }
        
        public int? PostId { get; set; }
        public Post Post { get; set; }
        
        public int? ProfileId { get; set; }
        public Profile Profile { get; set; }
        
        [Required]
        public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 