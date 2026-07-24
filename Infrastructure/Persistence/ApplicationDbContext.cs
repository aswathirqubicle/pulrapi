using Core.Application.Interfaces;
using Core.Domain.Entities;
using Core.Domain.Views;
using Core.Infrastructure.Common;
using Core.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<User>, IApplicationDbContext
    {
        private readonly EntitySaveChangesInterceptor _entitySaveChangesInterceptor;
        private readonly IMediator _mediator;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            EntitySaveChangesInterceptor entitySaveChangesInterceptor,
            IMediator mediator
        ) :
            base(options)
        {
            _entitySaveChangesInterceptor = entitySaveChangesInterceptor;
            _mediator = mediator;
        }

        public virtual DbSet<AppLog> AppLogs { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<CategoryClosure> CategoryClosures { get; set; }

        public virtual DbSet<SubCategoryLevel1> SubCategoryLevel1s { get; set; }
        public virtual DbSet<SubCategoryLevel2> SubCategoryLevel2s { get; set; }
        public virtual DbSet<ProductSubCategoryLevel2> ProductSubCategoryLevel2s { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<CommentLike> CommentLikes { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<Manufacturer> Manufacturers { get; set; }
        public virtual DbSet<MediaFile> MediaFiles { get; set; }
        public virtual DbSet<Post> Posts { get; set; }
        public virtual DbSet<PostClick> PostClicks { get; set; }
        public virtual DbSet<PostHashtag> PostHashtags { get; set; }
        public virtual DbSet<PostLike> PostLikes { get; set; }
        public virtual DbSet<PostProductTag> PostProductTags { get; set; }
        public virtual DbSet<PostProfileMention> PostProfileMentions { get; set; }
        public virtual DbSet<PostStoreMention> PostStoreMentions { get; set; }
        public virtual DbSet<PostMyStyle> PostMyStyles { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<Brand> Brands { get; set; }
        public virtual DbSet<ProductVariantOption> ProductVariantOptions { get; set; }
        public virtual DbSet<ProductVariant> ProductVariants { get; set; }
        public virtual DbSet<ProductLike> ProductLikes { get; set; }
        public virtual DbSet<ProductClick> ProductClicks { get; set; }
        public virtual DbSet<UserBagProduct> UserBagProducts { get; set; }
        public virtual DbSet<UserWishlistProduct> UserWishlistProducts { get; set; }
        public virtual DbSet<ProductMoreInfo> ProductMoreInfos { get; set; }
        public virtual DbSet<ProductPair> ProductPairs { get; set; }
        public virtual DbSet<ProductSimilar> ProductSimilars { get; set; }
        public virtual DbSet<ProductAttribute> ProductAttributes { get; set; }
        public virtual DbSet<ProductMediaFile> ProductMediaFiles { get; set; }
        public virtual DbSet<ProductVariantCombination> ProductVariantCombinations { get; set; }
        public virtual DbSet<ProductVariantCombinationOption> ProductVariantCombinationOptions { get; set; }
        public virtual DbSet<Profile> Profiles { get; set; }
        public virtual DbSet<FollowRequest> FollowRequests { get; set; }

        public virtual DbSet<ProfileSocialMedia> ProfileSocialMedias { get; set; }
        public virtual DbSet<StoreSocialMedia> StoreSocialMedias { get; set; }

        //Identity tables
        public  new virtual DbSet<IdentityRole> Roles { get; set; }
        public new virtual DbSet<IdentityUserRole<string>> UserRoles { get; set; }

        public virtual DbSet<Story> Stories { get; set; }
        public virtual DbSet<StorySeen> StorySeens { get; set; }
        public virtual DbSet<StoryHashTag> StoryHashTags { get; set; }
        public virtual DbSet<StoryProductTag> StoryProductTags { get; set; }
        public virtual DbSet<StoryProfileMention> StoryProfileMentions { get; set; }
        public virtual DbSet<StoryLike> StoryLikes { get; set; }
        public virtual DbSet<ProfileFollower> ProfileFollowers { get; set; }
        public virtual DbSet<OnboardingPreference> OnboardingPreferences { get; set; }
        public virtual DbSet<ProfileOnboardingPreference> ProfileOnboardingPreferences { get; set; }
        public virtual DbSet<ProductOnboardingPreference> ProductOnboardingPreferences { get; set; }
        public virtual DbSet<Vibe> Vibes { get; set; }
        public virtual DbSet<ProfileVibe> ProfileVibes { get; set; }
        public virtual DbSet<Store> Stores { get; set; }
        public virtual DbSet<StoreFollower> StoreFollowers { get; set; }
        public virtual DbSet<StoreProduct> StoreProducts { get; set; }
        public virtual DbSet<StoreRating> StoreRatings { get; set; }
        public virtual DbSet<Fulfillment> Fulfillments { get; set; }
        public virtual DbSet<Hashtag> Hashtags { get; set; }
        public virtual DbSet<Industry> Industries { get; set; }
        public virtual DbSet<StoreIndustry> StoreIndustries { get; set; }
        public virtual DbSet<Currency> Currencies { get; set; }
        public virtual DbSet<Affiliate> Affiliates { get; set; }
        public virtual DbSet<ProductCategory> ProductCategories { get; set; }
        public virtual DbSet<SearchHistory> SearchHistories { get; set; }
        public virtual DbSet<Gender> Genders { get; set; }
        public virtual DbSet<ProfileSocialMediaLink> ProfileSocialMediaLinks { get; set; }
        public virtual DbSet<ProfileSettings> ProfileSettings { get; set; }
        public virtual DbSet<SellerSettings> SellerSettings { get; set; }
        public virtual DbSet<UserLoginActivity> UserLoginActivities { get; set; }
        public virtual DbSet<UserNotificationSetting> UserNotificationSettings { get; set; }
        public virtual DbSet<Activity> Activities { get; set; }
        public virtual DbSet<NotificationHistory> NotificationHistories { get; set; }
        public virtual DbSet<Mention> Mentions { get; set; }
        public virtual DbSet<UserPushToken> UserPushTokens { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<RevokedToken> RevokedTokens { get; set; }
        public virtual DbSet<StripeWebhookEvent> StripeWebhookEvents { get; set; }
        public virtual DbSet<BookmarkCollection> BookmarkCollections { get; set; }
        public virtual DbSet<BookmarkCollectionItem> BookmarkCollectionItems { get; set; }


        #region Finance

        public virtual DbSet<GlobalCurrencySetting> GlobalCurrencySettings { get; set; }
        public virtual DbSet<ExchangeRate> ExchangeRates { get; set; }
        public virtual DbSet<ShippingDetails> ShippingDetails { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }
        public virtual DbSet<OrderProductAffiliate> OrderProductAffiliates { get; set; }
        public virtual DbSet<OrderItemShippingProof> OrderItemShippingProofs { get; set; }
        public virtual DbSet<UserBlock> UserBlocks { get; set; }
        public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }
        public virtual DbSet<Dispute> Disputes { get; set; }
        public virtual DbSet<PaymentFeeSetting> PaymentFeeSettings { get; set; }
        public virtual DbSet<PlatformSetting> PlatformSettings { get; set; }
        public virtual DbSet<EscrowWallet> EscrowWallets { get; set; }
        public virtual DbSet<EscrowWalletTransaction> EscrowWalletTransactions { get; set; }
        public virtual DbSet<RefundDispute> RefundDisputes { get; set; }
        public virtual DbSet<RefundDisputeEvidence> RefundDisputeEvidences { get; set; }

        #endregion


        #region Views

        public DbSet<ProfileFollowingView> ProfileFollowingViews { get; set; }

        public DbSet<Report> Reports { get; set; }
        #endregion


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(_entitySaveChangesInterceptor);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            await _mediator.DispatchDomainEvents(this);
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<ProfileFollowingView>().ToView("ProfileFollowingView").HasNoKey();

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(builder);

            // Configure UserBlock relationships
            builder.Entity<UserBlock>()
                .HasOne(ub => ub.BlockerProfile)
                .WithMany(p => p.BlockedUsers)
                .HasForeignKey(ub => ub.BlockerProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserBlock>()
                .HasOne(ub => ub.BlockedProfile)
                .WithMany(p => p.BlockedByUsers)
                .HasForeignKey(ub => ub.BlockedProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Revoked (logged-out) JWT tokens are looked up by jti on every
            // authenticated request, so the column is uniquely indexed.
            builder.Entity<RevokedToken>()
                .HasIndex(rt => rt.Jti)
                .IsUnique();

            // Stripe delivers webhooks at-least-once; the event id is the idempotency
            // key, so it is uniquely indexed to detect and reject duplicate deliveries.
            builder.Entity<StripeWebhookEvent>()
                .HasIndex(e => e.EventId)
                .IsUnique();

            // any config goes to persistence/configuration
        }
    }
}