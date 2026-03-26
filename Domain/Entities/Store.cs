
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Domain.Entities
{
    public class Store : EntityBase
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string LegalName { get; set; }
        [Required]
        public string UniqueName { get; set; }
        [Required]
        public string StoreEmail { get; set; }
        [Required]
        public bool IsEmailPublic { get; set; } = false;
        [Required]
        public string UserId { get; set; }
        public User User { get; set; }
        [Required]
        public Currency Currency { get; set; }
        public string Description { get; set; }
        public string About { get; set; }
        public string Location { get; set; }
        public string PhoneNumber { get; set; }
        public string ImageUrl { get; set; }
        public string BannerUrl { get; set; }
        public int LikesCount { get; set; }
        public bool TermsAccepted { get; set; }
        public bool IsVerified { get; set; }
        public Affiliate Affiliate { get; set; }
        public StoreSocialMedia StoreSocialMedia { get; set; }

        public virtual Fulfillment Fulfillment { get; set; }
        public virtual ICollection<StoreFollower> StoreFollowers { get; set; }
        public virtual ICollection<StoreRating> StoreRatings { get; set; }
        public virtual ICollection<StoreProduct> StoreProducts { get; set; }
        public virtual ICollection<StoreIndustry> StoreIndustries { get; set; }
        public virtual ICollection<ProductCategory> ProductCategories { get; set; }
        public virtual ICollection<Product> Products { get; set; }
        public virtual ICollection<Post> Posts { get; set; }
        public virtual ICollection<Story> Stories { get; set; } = new List<Story>();

        // this field is calculated on query, we ignore it in migration
        [NotMapped]
        public int? ProductsCount { get; set; }
    }
}
