using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistAndVariantSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostProductTags_Products_ProductId",
                table: "PostProductTags");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBagProducts_Affiliates_AffiliateId",
                table: "UserBagProducts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserBagProducts",
                table: "UserBagProducts");

            migrationBuilder.AddColumn<string>(
                name: "ProductVariantCombinationUid",
                table: "UserBagProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserBagProducts",
                table: "UserBagProducts",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ProductVariantCombinations_Uid",
                table: "ProductVariantCombinations",
                column: "Uid");

            migrationBuilder.CreateTable(
                name: "UserWishlistProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WishlistProductId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProductVariantCombinationUid = table.Column<string>(type: "text", nullable: true),
                    AffiliateId = table.Column<int>(type: "integer", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWishlistProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWishlistProducts_Affiliates_AffiliateId",
                        column: x => x.AffiliateId,
                        principalTable: "Affiliates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserWishlistProducts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserWishlistProducts_ProductVariantCombinations_ProductVari~",
                        column: x => x.ProductVariantCombinationUid,
                        principalTable: "ProductVariantCombinations",
                        principalColumn: "Uid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserWishlistProducts_Products_WishlistProductId",
                        column: x => x.WishlistProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBagProducts_ProductVariantCombinationUid",
                table: "UserBagProducts",
                column: "ProductVariantCombinationUid");

            migrationBuilder.CreateIndex(
                name: "IX_UserBagProducts_UserId_BagProductId",
                table: "UserBagProducts",
                columns: new[] { "UserId", "BagProductId" },
                unique: true,
                filter: "\"ProductVariantCombinationUid\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserBagProducts_UserId_BagProductId_ProductVariantCombinati~",
                table: "UserBagProducts",
                columns: new[] { "UserId", "BagProductId", "ProductVariantCombinationUid" },
                unique: true,
                filter: "\"ProductVariantCombinationUid\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserWishlistProducts_AffiliateId",
                table: "UserWishlistProducts",
                column: "AffiliateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWishlistProducts_ProductVariantCombinationUid",
                table: "UserWishlistProducts",
                column: "ProductVariantCombinationUid");

            migrationBuilder.CreateIndex(
                name: "IX_UserWishlistProducts_Uid",
                table: "UserWishlistProducts",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_UserWishlistProducts_UserId_WishlistProductId",
                table: "UserWishlistProducts",
                columns: new[] { "UserId", "WishlistProductId" },
                unique: true,
                filter: "\"ProductVariantCombinationUid\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserWishlistProducts_UserId_WishlistProductId_ProductVarian~",
                table: "UserWishlistProducts",
                columns: new[] { "UserId", "WishlistProductId", "ProductVariantCombinationUid" },
                unique: true,
                filter: "\"ProductVariantCombinationUid\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserWishlistProducts_WishlistProductId",
                table: "UserWishlistProducts",
                column: "WishlistProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostProductTags_Products_ProductId",
                table: "PostProductTags",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBagProducts_Affiliates_AffiliateId",
                table: "UserBagProducts",
                column: "AffiliateId",
                principalTable: "Affiliates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBagProducts_ProductVariantCombinations_ProductVariantCo~",
                table: "UserBagProducts",
                column: "ProductVariantCombinationUid",
                principalTable: "ProductVariantCombinations",
                principalColumn: "Uid",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostProductTags_Products_ProductId",
                table: "PostProductTags");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBagProducts_Affiliates_AffiliateId",
                table: "UserBagProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBagProducts_ProductVariantCombinations_ProductVariantCo~",
                table: "UserBagProducts");

            migrationBuilder.DropTable(
                name: "UserWishlistProducts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserBagProducts",
                table: "UserBagProducts");

            migrationBuilder.DropIndex(
                name: "IX_UserBagProducts_ProductVariantCombinationUid",
                table: "UserBagProducts");

            migrationBuilder.DropIndex(
                name: "IX_UserBagProducts_UserId_BagProductId",
                table: "UserBagProducts");

            migrationBuilder.DropIndex(
                name: "IX_UserBagProducts_UserId_BagProductId_ProductVariantCombinati~",
                table: "UserBagProducts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ProductVariantCombinations_Uid",
                table: "ProductVariantCombinations");

            migrationBuilder.DropColumn(
                name: "ProductVariantCombinationUid",
                table: "UserBagProducts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserBagProducts",
                table: "UserBagProducts",
                columns: new[] { "UserId", "BagProductId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PostProductTags_Products_ProductId",
                table: "PostProductTags",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBagProducts_Affiliates_AffiliateId",
                table: "UserBagProducts",
                column: "AffiliateId",
                principalTable: "Affiliates",
                principalColumn: "Id");
        }
    }
}
