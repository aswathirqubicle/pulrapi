using Core.Domain.Entities;
using Core.Domain.Views;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        public DbSet<AppLog> AppLogs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CategoryClosure> CategoryClosures { get; set; }
        public DbSet<SubCategoryLevel1> SubCategoryLevel1s { get; set; }
        public DbSet<SubCategoryLevel2> SubCategoryLevel2s { get; set; }
        public DbSet<ProductSubCategoryLevel2> ProductSubCategoryLevel2s { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentLike> CommentLikes { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostClick> PostClicks { get; set; }
        public DbSet<PostHashtag> PostHashtags { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<PostProductTag> PostProductTags { get; set; }
        public DbSet<PostProfileMention> PostProfileMentions { get; set; }
        public DbSet<PostStoreMention> PostStoreMentions { get; set; }
        public DbSet<PostMyStyle> PostMyStyles { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<ProductLike> ProductLikes { get; set; }
        public DbSet<ProductClick> ProductClicks { get; set; }
        public DbSet<ProductVariantOption> ProductVariantOptions { get; set; }
        public DbSet<UserBagProduct> UserBagProducts { get; set; }
        public DbSet<UserWishlistProduct> UserWishlistProducts { get; set; }
        public DbSet<ProductMoreInfo> ProductMoreInfos { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductMediaFile> ProductMediaFiles { get; set; }
        public DbSet<ProductPair> ProductPairs { get; set; }
        public DbSet<ProductSimilar> ProductSimilars { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductVariantCombination> ProductVariantCombinations { get; set; }
        public DbSet<ProductVariantCombinationOption> ProductVariantCombinationOptions { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<FollowRequest> FollowRequests { get; set; }
        public DbSet<ProfileSocialMedia> ProfileSocialMedias { get; set; }
        public DbSet<StoreSocialMedia> StoreSocialMedias { get; set; }
        public DbSet<Story> Stories { get; set; }
        public DbSet<StorySeen> StorySeens { get; set; }
        public DbSet<StoryLike> StoryLikes { get; set; }
        public DbSet<StoryHashTag> StoryHashTags { get; set; }
        public DbSet<StoryProductTag> StoryProductTags { get; set; }
        public DbSet<StoryProfileMention> StoryProfileMentions { get; set; }
        public DbSet<ProfileFollower> ProfileFollowers { get; set; }
        public DbSet<ProfileFollowingView> ProfileFollowingViews { get; set; }
        public DbSet<OnboardingPreference> OnboardingPreferences { get; set; }
        public DbSet<ProfileOnboardingPreference> ProfileOnboardingPreferences { get; set; }
        public DbSet<ProductOnboardingPreference> ProductOnboardingPreferences { get; set; }
        public DbSet<Vibe> Vibes { get; set; }
        public DbSet<ProfileVibe> ProfileVibes { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<StoreFollower> StoreFollowers { get; set; }
        public DbSet<StoreProduct> StoreProducts { get; set; }
        public DbSet<StoreRating> StoreRatings { get; set; }
        public DbSet<Industry> Industries { get; set; }
        public DbSet<StoreIndustry> StoreIndustries { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Fulfillment> Fulfillments { get; set; }
        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Affiliate> Affiliates { get; set; }
        public DbSet<OrderProductAffiliate> OrderProductAffiliates { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }
        public DbSet<Gender> Genders { get; set; }
        //add report
        public DbSet<Report> Reports { get; set; }

        //Identity tables
        public DbSet<IdentityRole> Roles { get; set; }
        public DbSet<IdentityUserRole<string>> UserRoles { get; set; }

        public DbSet<UserBlock> UserBlocks { get; set; }
        public DbSet<ProfileSocialMediaLink> ProfileSocialMediaLinks { get; set; }
        public DbSet<ProfileSettings> ProfileSettings { get; set; }
        public DbSet<SellerSettings> SellerSettings { get; set; }
        public DbSet<UserLoginActivity> UserLoginActivities { get; set; }
        public DbSet<UserNotificationSetting> UserNotificationSettings { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<NotificationHistory> NotificationHistories { get; set; }
        public DbSet<Mention> Mentions { get; set; }
        public DbSet<UserPushToken> UserPushTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<BookmarkCollection> BookmarkCollections { get; set; }
        public DbSet<BookmarkCollectionItem> BookmarkCollectionItems { get; set; }


        #region Finance

        public DbSet<GlobalCurrencySetting> GlobalCurrencySettings { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<ShippingDetails> ShippingDetails { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Dispute> Disputes { get; set; }

        #endregion

        public DbSet<TEntity> Set<TEntity>() where TEntity : class;

        Task<int>  SaveChangesAsync(CancellationToken cancellationToken);
        int SaveChanges();
    }
}