using Core.Application.Interfaces;
using Core.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Mediatr.Users.Commands.AdminDelete
{
    public class AdminDeleteUserCommandHandler : IRequestHandler<AdminDeleteUserCommand, AdminDeleteUserResponse>
    {
        private readonly IApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AdminDeleteUserCommandHandler> _logger;
        private const string SecretCode = "RlupTestmydb2811";

        public AdminDeleteUserCommandHandler(
            IApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<AdminDeleteUserCommandHandler> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<AdminDeleteUserResponse> Handle(AdminDeleteUserCommand request, CancellationToken cancellationToken)
        {
            // Validate secret code
            if (request.SecretCode != SecretCode)
            {
                return new AdminDeleteUserResponse
                {
                    Success = false,
                    Message = "Invalid secret code."
                };
            }

            // Find user
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null)
            {
                return new AdminDeleteUserResponse
                {
                    Success = false,
                    Message = "User not found."
                };
            }

            try
            {
                var userId = user.Id;
                _logger.LogInformation("Starting deletion process for user {UserId} ({Username})", userId, request.Username);

                // Get database context
                var db = _context as DbContext;
                if (db == null)
                {
                    throw new InvalidOperationException("Cannot access database context");
                }

                // Execute all deletions using raw SQL in the correct order
                await ExecuteDeleteCommands(db, userId, cancellationToken);

                // Finally, delete the user using Identity
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to delete user {UserId}: {Errors}", userId, errors);
                    return new AdminDeleteUserResponse
                    {
                        Success = false,
                        Message = $"Failed to delete user: {errors}"
                    };
                }

                _logger.LogInformation("Successfully deleted user {UserId} ({Username}) and all related data", userId, request.Username);

                return new AdminDeleteUserResponse
                {
                    Success = true,
                    Message = $"User {request.Username} and all related data deleted successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Username}", request.Username);
                var innerMessage = ex.InnerException?.Message ?? string.Empty;
                return new AdminDeleteUserResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}. Inner: {innerMessage}"
                };
            }
        }

        private async Task ExecuteDeleteCommands(DbContext db, string userId, CancellationToken cancellationToken)
        {
            // Helper to execute SQL safely
            async Task<int> ExecuteSql(string sql, string description)
            {
                try
                {
                    var affected = await db.Database.ExecuteSqlRawAsync(sql, new object[] { userId }, cancellationToken);
                    if (affected > 0)
                    {
                        _logger.LogInformation("{Description}: Deleted {Count} rows", description, affected);
                    }
                    return affected;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Warning for {Description}: {Message}", description, ex.Message);
                    return 0;
                }
            }

            _logger.LogInformation("Starting deletion for user {UserId}", userId);

            // ============================================
            // STEP 1: Delete OrderProductAffiliates FIRST
            // This table has FK to both Products AND Orders
            // Products can be linked via UserId directly OR via StoreId
            // ============================================
            _logger.LogInformation("Step 1: Deleting OrderProductAffiliates...");
            
            // Delete by products linked directly to user
            await ExecuteSql(
                @"DELETE FROM ""OrderProductAffiliates"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})",
                "OrderProductAffiliates (by product UserId)");
            
            // Delete by products linked through stores
            await ExecuteSql(
                @"DELETE FROM ""OrderProductAffiliates"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})",
                "OrderProductAffiliates (by product StoreId)");
            
            // Delete by orders placed by this user's profiles
            await ExecuteSql(
                @"DELETE FROM ""OrderProductAffiliates"" WHERE ""OrderId"" IN (SELECT o.""Id"" FROM ""Orders"" o INNER JOIN ""Profiles"" pr ON o.""ProfileId"" = pr.""Id"" WHERE pr.""UserId"" = {0})",
                "OrderProductAffiliates (by order)");

            // ============================================
            // STEP 2: Delete WalletTransactions (FK to Orders)
            // ============================================
            _logger.LogInformation("Step 2: Deleting WalletTransactions...");
            
            await ExecuteSql(
                @"DELETE FROM ""WalletTransactions"" WHERE ""OrderId"" IN (SELECT o.""Id"" FROM ""Orders"" o INNER JOIN ""Profiles"" pr ON o.""ProfileId"" = pr.""Id"" WHERE pr.""UserId"" = {0})",
                "WalletTransactions (by order)");
            
            await ExecuteSql(
                @"DELETE FROM ""WalletTransactions"" WHERE ""UserId"" = {0}",
                "WalletTransactions (by user)");

            // ============================================
            // STEP 3: Delete Disputes
            // ============================================
            await ExecuteSql(@"DELETE FROM ""Disputes"" WHERE ""UserId"" = {0}", "Disputes");

            // ============================================
            // STEP 4: Delete BookmarkCollectionItems & BookmarkCollections
            // ============================================
            await ExecuteSql(
                @"DELETE FROM ""BookmarkCollectionItems"" WHERE ""BookmarkCollectionId"" IN (SELECT bc.""Id"" FROM ""BookmarkCollections"" bc INNER JOIN ""Profiles"" p ON bc.""ProfileId"" = p.""Id"" WHERE p.""UserId"" = {0})",
                "BookmarkCollectionItems");
            
            await ExecuteSql(
                @"DELETE FROM ""BookmarkCollections"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})",
                "BookmarkCollections");

            // ============================================
            // STEP 5: Delete Orders
            // ============================================
            _logger.LogInformation("Step 5: Deleting Orders...");
            await ExecuteSql(
                @"DELETE FROM ""Orders"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})",
                "Orders");

            // ============================================
            // STEP 6: Delete Story related entities
            // ============================================
            _logger.LogInformation("Step 6: Deleting Story related...");
            await ExecuteSql(@"DELETE FROM ""StorySeens"" WHERE ""StoryId"" IN (SELECT ""Id"" FROM ""Stories"" WHERE ""UserId"" = {0})", "StorySeens");
            await ExecuteSql(@"DELETE FROM ""StoryHashTags"" WHERE ""StoryId"" IN (SELECT ""Id"" FROM ""Stories"" WHERE ""UserId"" = {0})", "StoryHashTags");
            await ExecuteSql(@"DELETE FROM ""StoryProductTags"" WHERE ""StoryId"" IN (SELECT ""Id"" FROM ""Stories"" WHERE ""UserId"" = {0})", "StoryProductTags");
            await ExecuteSql(@"DELETE FROM ""StoryProfileMentions"" WHERE ""StoryId"" IN (SELECT ""Id"" FROM ""Stories"" WHERE ""UserId"" = {0})", "StoryProfileMentions");
            await ExecuteSql(@"DELETE FROM ""StoryLikes"" WHERE ""StoryId"" IN (SELECT ""Id"" FROM ""Stories"" WHERE ""UserId"" = {0})", "StoryLikes (by story)");
            await ExecuteSql(@"DELETE FROM ""StoryLikes"" WHERE ""LikedById"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "StoryLikes (by profile)");
            await ExecuteSql(@"DELETE FROM ""Stories"" WHERE ""UserId"" = {0}", "Stories");

            // ============================================
            // STEP 7: Delete Comment related entities
            // ============================================
            _logger.LogInformation("Step 7: Deleting Comment related...");
            await ExecuteSql(@"DELETE FROM ""CommentLikes"" WHERE ""CommentId"" IN (SELECT c.""Id"" FROM ""Comments"" c INNER JOIN ""Profiles"" p ON c.""CommentedById"" = p.""Id"" WHERE p.""UserId"" = {0})", "CommentLikes (user comments)");
            await ExecuteSql(@"DELETE FROM ""Comments"" WHERE ""CommentedById"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "Comments (by user)");

            // ============================================
            // STEP 8: Delete Post related entities
            // ============================================
            _logger.LogInformation("Step 8: Deleting Post related...");
            await ExecuteSql(@"DELETE FROM ""PostHashtags"" WHERE ""PostId"" IN (SELECT ""Id"" FROM ""Posts"" WHERE ""UserId"" = {0})", "PostHashtags");
            await ExecuteSql(@"DELETE FROM ""PostProductTags"" WHERE ""PostId"" IN (SELECT ""Id"" FROM ""Posts"" WHERE ""UserId"" = {0})", "PostProductTags");
            await ExecuteSql(@"DELETE FROM ""PostLikes"" WHERE ""PostId"" IN (SELECT ""Id"" FROM ""Posts"" WHERE ""UserId"" = {0})", "PostLikes");
            await ExecuteSql(@"DELETE FROM ""PostMyStyles"" WHERE ""PostId"" IN (SELECT ""Id"" FROM ""Posts"" WHERE ""UserId"" = {0})", "PostMyStyles (by post)");
            await ExecuteSql(@"DELETE FROM ""PostMyStyles"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "PostMyStyles (by profile)");
            await ExecuteSql(@"DELETE FROM ""PostProfileMentions"" WHERE ""PostId"" IN (SELECT ""Id"" FROM ""Posts"" WHERE ""UserId"" = {0})", "PostProfileMentions");
            await ExecuteSql(@"DELETE FROM ""PostStoreMentions"" WHERE ""PostId"" IN (SELECT ""Id"" FROM ""Posts"" WHERE ""UserId"" = {0})", "PostStoreMentions");
            await ExecuteSql(@"DELETE FROM ""PostClicks"" WHERE ""UserId"" = {0}", "PostClicks");
            await ExecuteSql(@"DELETE FROM ""Comments"" WHERE ""PostId"" IN (SELECT ""Id"" FROM ""Posts"" WHERE ""UserId"" = {0})", "Comments (on posts)");
            await ExecuteSql(@"DELETE FROM ""Posts"" WHERE ""UserId"" = {0}", "Posts");

            // ============================================
            // STEP 9: Delete ALL Product related entities BEFORE Products
            // Products can be linked via UserId directly OR via StoreId
            // ============================================
            _logger.LogInformation("Step 9: Deleting Product related...");
            
            // Delete by products linked DIRECTLY to user (UserId column)
            await ExecuteSql(@"DELETE FROM ""ProductVariantCombinationOptions"" WHERE ""ProductVariantCombinationId"" IN (SELECT pvc.""Id"" FROM ""ProductVariantCombinations"" pvc INNER JOIN ""Products"" p ON pvc.""ProductId"" = p.""Id"" WHERE p.""UserId"" = {0})", "ProductVariantCombinationOptions (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductVariantCombinations"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductVariantCombinations (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductVariantOptions"" WHERE ""ProductVariantId"" IN (SELECT pv.""Id"" FROM ""ProductVariants"" pv INNER JOIN ""Products"" p ON pv.""ProductId"" = p.""Id"" WHERE p.""UserId"" = {0})", "ProductVariantOptions (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductVariants"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductVariants (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductSimilars"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductSimilars (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductPairs"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductPairs (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductMediaFiles"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductMediaFiles (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductLikes"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductLikes (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductClicks"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductClicks (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductCategories"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductCategories (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductOnboardingPreferences"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductOnboardingPreferences (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductSubCategoryLevel2s"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductSubCategoryLevel2s (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductAttributes"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductAttributes (by UserId)");
            await ExecuteSql(@"DELETE FROM ""ProductMoreInfos"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "ProductMoreInfos (by UserId)");
            await ExecuteSql(@"DELETE FROM ""UserWishlistProducts"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "UserWishlistProducts (by UserId)");
            await ExecuteSql(@"DELETE FROM ""StoreProducts"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "StoreProducts (by UserId)");
            await ExecuteSql(@"DELETE FROM ""StoryProductTags"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "StoryProductTags (by UserId)");
            await ExecuteSql(@"DELETE FROM ""PostProductTags"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "PostProductTags (by UserId)");
            await ExecuteSql(@"DELETE FROM ""CommentLikes"" WHERE ""CommentId"" IN (SELECT c.""Id"" FROM ""Comments"" c INNER JOIN ""Products"" p ON c.""ProductId"" = p.""Id"" WHERE p.""UserId"" = {0})", "CommentLikes (product comments by UserId)");
            await ExecuteSql(@"DELETE FROM ""Comments"" WHERE ""ProductId"" IN (SELECT ""Id"" FROM ""Products"" WHERE ""UserId"" = {0})", "Comments (on products by UserId)");

            // Delete by products linked through STORES (StoreId column)
            await ExecuteSql(@"DELETE FROM ""ProductVariantCombinationOptions"" WHERE ""ProductVariantCombinationId"" IN (SELECT pvc.""Id"" FROM ""ProductVariantCombinations"" pvc INNER JOIN ""Products"" p ON pvc.""ProductId"" = p.""Id"" INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductVariantCombinationOptions (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductVariantCombinations"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductVariantCombinations (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductVariantOptions"" WHERE ""ProductVariantId"" IN (SELECT pv.""Id"" FROM ""ProductVariants"" pv INNER JOIN ""Products"" p ON pv.""ProductId"" = p.""Id"" INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductVariantOptions (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductVariants"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductVariants (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductSimilars"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductSimilars (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductPairs"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductPairs (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductMediaFiles"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductMediaFiles (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductLikes"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductLikes (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductClicks"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductClicks (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductCategories"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductCategories (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductOnboardingPreferences"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductOnboardingPreferences (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductSubCategoryLevel2s"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductSubCategoryLevel2s (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductAttributes"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductAttributes (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""ProductMoreInfos"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "ProductMoreInfos (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""UserWishlistProducts"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "UserWishlistProducts (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""StoreProducts"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "StoreProducts (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""StoryProductTags"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "StoryProductTags (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""PostProductTags"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "PostProductTags (by StoreId)");
            await ExecuteSql(@"DELETE FROM ""CommentLikes"" WHERE ""CommentId"" IN (SELECT c.""Id"" FROM ""Comments"" c INNER JOIN ""Products"" p ON c.""ProductId"" = p.""Id"" INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "CommentLikes (product comments by StoreId)");
            await ExecuteSql(@"DELETE FROM ""Comments"" WHERE ""ProductId"" IN (SELECT p.""Id"" FROM ""Products"" p INNER JOIN ""Stores"" s ON p.""StoreId"" = s.""Id"" WHERE s.""UserId"" = {0})", "Comments (on products by StoreId)");

            // ============================================
            // STEP 10: Delete Products (by UserId AND by StoreId)
            // ============================================
            _logger.LogInformation("Step 10: Deleting Products...");
            await ExecuteSql(@"DELETE FROM ""Products"" WHERE ""UserId"" = {0}", "Products (by UserId)");
            await ExecuteSql(@"DELETE FROM ""Products"" WHERE ""StoreId"" IN (SELECT ""Id"" FROM ""Stores"" WHERE ""UserId"" = {0})", "Products (by StoreId)");

            // ============================================
            // STEP 11: Delete Store related entities
            // ============================================
            _logger.LogInformation("Step 11: Deleting Store related...");
            await ExecuteSql(@"DELETE FROM ""StoreFollowers"" WHERE ""StoreId"" IN (SELECT ""Id"" FROM ""Stores"" WHERE ""UserId"" = {0})", "StoreFollowers");
            await ExecuteSql(@"DELETE FROM ""StoreRatings"" WHERE ""StoreId"" IN (SELECT ""Id"" FROM ""Stores"" WHERE ""UserId"" = {0})", "StoreRatings");
            await ExecuteSql(@"DELETE FROM ""StoreSocialMedias"" WHERE ""StoreId"" IN (SELECT ""Id"" FROM ""Stores"" WHERE ""UserId"" = {0})", "StoreSocialMedias");
            await ExecuteSql(@"DELETE FROM ""StoreIndustries"" WHERE ""StoreId"" IN (SELECT ""Id"" FROM ""Stores"" WHERE ""UserId"" = {0})", "StoreIndustries");
            await ExecuteSql(@"DELETE FROM ""SellerSettings"" WHERE ""StoreId"" IN (SELECT ""Id"" FROM ""Stores"" WHERE ""UserId"" = {0})", "SellerSettings");
            await ExecuteSql(@"DELETE FROM ""StoreProducts"" WHERE ""StoreId"" IN (SELECT ""Id"" FROM ""Stores"" WHERE ""UserId"" = {0})", "StoreProducts (by store)");
            await ExecuteSql(@"DELETE FROM ""Stores"" WHERE ""UserId"" = {0}", "Stores");

            // ============================================
            // STEP 12: Delete Profile related entities
            // ============================================
            _logger.LogInformation("Step 12: Deleting Profile related...");
            await ExecuteSql(@"DELETE FROM ""ProfileFollowers"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0}) OR ""FollowerId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "ProfileFollowers");
            await ExecuteSql(@"DELETE FROM ""FollowRequests"" WHERE ""RequesterId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0}) OR ""RequestedId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "FollowRequests");
            await ExecuteSql(@"DELETE FROM ""ProfileSettings"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "ProfileSettings");
            await ExecuteSql(@"DELETE FROM ""ProfileSocialMedias"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "ProfileSocialMedias");
            await ExecuteSql(@"DELETE FROM ""ProfileSocialMediaLinks"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "ProfileSocialMediaLinks");
            await ExecuteSql(@"DELETE FROM ""ProfileOnboardingPreferences"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "ProfileOnboardingPreferences");
            await ExecuteSql(@"DELETE FROM ""ProfileVibes"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "ProfileVibes");
            await ExecuteSql(@"DELETE FROM ""Reports"" WHERE ""ProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "Reports");
            await ExecuteSql(@"DELETE FROM ""UserBlocks"" WHERE ""BlockerProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0}) OR ""BlockedProfileId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "UserBlocks");
            await ExecuteSql(@"DELETE FROM ""NotificationHistories"" WHERE ""ActorUserId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0}) OR ""ReceiverUserId"" IN (SELECT ""Id"" FROM ""Profiles"" WHERE ""UserId"" = {0})", "NotificationHistories");
            await ExecuteSql(@"DELETE FROM ""Profiles"" WHERE ""UserId"" = {0}", "Profiles");

            // ============================================
            // STEP 13: Delete User specific data
            // ============================================
            _logger.LogInformation("Step 13: Deleting User specific data...");
            await ExecuteSql(@"DELETE FROM ""UserLoginActivities"" WHERE ""UserId"" = {0}", "UserLoginActivities");
            await ExecuteSql(@"DELETE FROM ""RefreshTokens"" WHERE ""UserId"" = {0}", "RefreshTokens");
            await ExecuteSql(@"DELETE FROM ""UserPushTokens"" WHERE ""UserId"" = {0}", "UserPushTokens");
            await ExecuteSql(@"DELETE FROM ""SearchHistories"" WHERE ""UserId"" = {0}", "SearchHistories");
            await ExecuteSql(@"DELETE FROM ""Activities"" WHERE ""UserId"" = {0}", "Activities");
            await ExecuteSql(@"DELETE FROM ""UserNotificationSettings"" WHERE ""UserId"" = {0}", "UserNotificationSettings");
            await ExecuteSql(@"DELETE FROM ""UserBagProducts"" WHERE ""UserId"" = {0}", "UserBagProducts");
            await ExecuteSql(@"DELETE FROM ""ShippingDetails"" WHERE ""UserId"" = {0}", "ShippingDetails");

            _logger.LogInformation("Completed deletion of all related data for user {UserId}", userId);
        }
    }
}
