using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints that might not exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_Bookmarks_BookmarkCollections_BookmarkCollectionId'
                    ) THEN
                        ALTER TABLE ""Bookmarks"" DROP CONSTRAINT ""FK_Bookmarks_BookmarkCollections_BookmarkCollectionId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderProductAffiliates_Products_ProductId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Products_ProductId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductLikes_Products_ProductId",
                table: "ProductLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOnboardingPreferences_Products_ProductId",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPairs_Products_ProductId",
                table: "ProductPairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Brand_BrandId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSimilars_Products_ProductId",
                table: "ProductSimilars");

            migrationBuilder.DropTable(
                name: "ProductAttributeValue");

            migrationBuilder.DropTable(
                name: "ProductVariation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductSimilars",
                table: "ProductSimilars");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductPairs",
                table: "ProductPairs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductOnboardingPreferences",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductMediaFiles",
                table: "ProductMediaFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductLikes",
                table: "ProductLikes");

            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_ProductId",
                table: "OrderProductAffiliates");

            // Drop index that might not exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM pg_indexes 
                        WHERE indexname = 'IX_Bookmarks_BookmarkCollectionId'
                    ) THEN
                        DROP INDEX ""IX_Bookmarks_BookmarkCollectionId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Brand",
                table: "Brand");

            migrationBuilder.DropColumn(
                name: "ProductCategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Products");

            // Drop column that might not exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Bookmarks' AND column_name = 'BookmarkCollectionId'
                    ) THEN
                        ALTER TABLE ""Bookmarks"" DROP COLUMN ""BookmarkCollectionId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.RenameTable(
                name: "Brand",
                newName: "Brands");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Products",
                newName: "MinPrice");

            migrationBuilder.RenameColumn(
                name: "ArticleCode",
                table: "Products",
                newName: "WhatIsIt");

            migrationBuilder.RenameIndex(
                name: "IX_Brand_Uid",
                table: "Brands",
                newName: "IX_Brands_Uid");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "UserBagProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "UserBagProducts",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserBagProducts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "UserBagProducts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "UserBagProducts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "UserBagProducts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "StoreRatings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "StoreRatings",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StoreRatings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "StoreRatings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "StoreRatings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StoreRatings",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "RefreshTokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RefreshTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "RefreshTokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "RefreshTokens",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProfileOnboardingPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProfileOnboardingPreferences",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProfileOnboardingPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "ProfileOnboardingPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "ProfileOnboardingPreferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProfileOnboardingPreferences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProductSimilars",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductSimilars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductSimilars",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "ProductSimilars",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "ProductSimilars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductSimilars",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxPrice",
                table: "Products",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ProductUrl",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProductPairs",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductPairs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductPairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "ProductPairs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "ProductPairs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductPairs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProductOnboardingPreferences",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductOnboardingPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductOnboardingPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "ProductOnboardingPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "ProductOnboardingPreferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductOnboardingPreferences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProductMediaFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductMediaFiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductMediaFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "ProductMediaFiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "ProductMediaFiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductMediaFiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProductLikes",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductLikes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProductLikes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "ProductLikes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "ProductLikes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductLikes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductSimilars",
                table: "ProductSimilars",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductPairs",
                table: "ProductPairs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductOnboardingPreferences",
                table: "ProductOnboardingPreferences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductMediaFiles",
                table: "ProductMediaFiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductLikes",
                table: "ProductLikes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Brands",
                table: "Brands",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductVariantId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ProductAttributeId = table.Column<int>(type: "integer", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantOptions_ProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductVariantOptions_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBagProducts_Uid",
                table: "UserBagProducts",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_StoreRatings_Uid",
                table: "StoreRatings",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Uid",
                table: "RefreshTokens",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileOnboardingPreferences_Uid",
                table: "ProfileOnboardingPreferences",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSimilars_ProductId",
                table: "ProductSimilars",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSimilars_Uid",
                table: "ProductSimilars",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_Products_UserId",
                table: "Products",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPairs_ProductId",
                table: "ProductPairs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPairs_Uid",
                table: "ProductPairs",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOnboardingPreferences_ProductId",
                table: "ProductOnboardingPreferences",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOnboardingPreferences_Uid",
                table: "ProductOnboardingPreferences",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaFiles_ProductId",
                table: "ProductMediaFiles",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMediaFiles_Uid",
                table: "ProductMediaFiles",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLikes_ProductId",
                table: "ProductLikes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLikes_Uid",
                table: "ProductLikes",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_ProductId",
                table: "OrderProductAffiliates",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptions_ProductAttributeId",
                table: "ProductVariantOptions",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptions_ProductVariantId",
                table: "ProductVariantOptions",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptions_Uid",
                table: "ProductVariantOptions",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Uid",
                table: "ProductVariants",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProductAffiliates_Products_ProductId",
                table: "OrderProductAffiliates",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Products_ProductId",
                table: "ProductCategories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductLikes_Products_ProductId",
                table: "ProductLikes",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOnboardingPreferences_Products_ProductId",
                table: "ProductOnboardingPreferences",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPairs_Products_ProductId",
                table: "ProductPairs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_UserId",
                table: "Products",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products",
                column: "BrandId",
                principalTable: "Brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSimilars_Products_ProductId",
                table: "ProductSimilars",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderProductAffiliates_Products_ProductId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCategories_Products_ProductId",
                table: "ProductCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductLikes_Products_ProductId",
                table: "ProductLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOnboardingPreferences_Products_ProductId",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductPairs_Products_ProductId",
                table: "ProductPairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_UserId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Brands_BrandId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSimilars_Products_ProductId",
                table: "ProductSimilars");

            migrationBuilder.DropTable(
                name: "ProductVariantOptions");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_UserBagProducts_Uid",
                table: "UserBagProducts");

            migrationBuilder.DropIndex(
                name: "IX_StoreRatings_Uid",
                table: "StoreRatings");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Uid",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_ProfileOnboardingPreferences_Uid",
                table: "ProfileOnboardingPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductSimilars",
                table: "ProductSimilars");

            migrationBuilder.DropIndex(
                name: "IX_ProductSimilars_ProductId",
                table: "ProductSimilars");

            migrationBuilder.DropIndex(
                name: "IX_ProductSimilars_Uid",
                table: "ProductSimilars");

            migrationBuilder.DropIndex(
                name: "IX_Products_UserId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductPairs",
                table: "ProductPairs");

            migrationBuilder.DropIndex(
                name: "IX_ProductPairs_ProductId",
                table: "ProductPairs");

            migrationBuilder.DropIndex(
                name: "IX_ProductPairs_Uid",
                table: "ProductPairs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductOnboardingPreferences",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropIndex(
                name: "IX_ProductOnboardingPreferences_ProductId",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropIndex(
                name: "IX_ProductOnboardingPreferences_Uid",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductMediaFiles",
                table: "ProductMediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_ProductMediaFiles_ProductId",
                table: "ProductMediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_ProductMediaFiles_Uid",
                table: "ProductMediaFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductLikes",
                table: "ProductLikes");

            migrationBuilder.DropIndex(
                name: "IX_ProductLikes_ProductId",
                table: "ProductLikes");

            migrationBuilder.DropIndex(
                name: "IX_ProductLikes_Uid",
                table: "ProductLikes");

            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_ProductId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Brands",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UserBagProducts");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserBagProducts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserBagProducts");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "UserBagProducts");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "UserBagProducts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "UserBagProducts");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "StoreRatings");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "StoreRatings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StoreRatings");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "StoreRatings");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "StoreRatings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StoreRatings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProfileOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProfileOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProfileOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "ProfileOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProfileOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProfileOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProductSimilars");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductSimilars");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductSimilars");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "ProductSimilars");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProductSimilars");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductSimilars");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductUrl",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProductPairs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductPairs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductPairs");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "ProductPairs");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProductPairs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductPairs");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductOnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProductMediaFiles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductMediaFiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductMediaFiles");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "ProductMediaFiles");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProductMediaFiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductMediaFiles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProductLikes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductLikes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProductLikes");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "ProductLikes");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProductLikes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductLikes");

            migrationBuilder.RenameTable(
                name: "Brands",
                newName: "Brand");

            migrationBuilder.RenameColumn(
                name: "WhatIsIt",
                table: "Products",
                newName: "ArticleCode");

            migrationBuilder.RenameColumn(
                name: "MinPrice",
                table: "Products",
                newName: "Price");

            migrationBuilder.RenameIndex(
                name: "IX_Brands_Uid",
                table: "Brand",
                newName: "IX_Brand_Uid");

            migrationBuilder.AddColumn<int>(
                name: "ProductCategoryId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BookmarkCollectionId",
                table: "Bookmarks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductSimilars",
                table: "ProductSimilars",
                columns: new[] { "ProductId", "SimilarId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductPairs",
                table: "ProductPairs",
                columns: new[] { "ProductId", "PairId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductOnboardingPreferences",
                table: "ProductOnboardingPreferences",
                columns: new[] { "ProductId", "OnboardingPreferenceId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductMediaFiles",
                table: "ProductMediaFiles",
                columns: new[] { "ProductId", "MediaFileId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductLikes",
                table: "ProductLikes",
                columns: new[] { "ProductId", "LikedById" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Brand",
                table: "Brand",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ProductVariation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariation_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductAttributeId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    ProductVariationId = table.Column<int>(type: "integer", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValue_ProductAttributes_ProductAttributeId",
                        column: x => x.ProductAttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValue_ProductVariation_ProductVariationId",
                        column: x => x.ProductVariationId,
                        principalTable: "ProductVariation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_ProductId",
                table: "OrderProductAffiliates",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_BookmarkCollectionId",
                table: "Bookmarks",
                column: "BookmarkCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValue_ProductAttributeId",
                table: "ProductAttributeValue",
                column: "ProductAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValue_ProductVariationId",
                table: "ProductAttributeValue",
                column: "ProductVariationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValue_Uid",
                table: "ProductAttributeValue",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariation_ProductId",
                table: "ProductVariation",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariation_Uid",
                table: "ProductVariation",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookmarks_BookmarkCollections_BookmarkCollectionId",
                table: "Bookmarks",
                column: "BookmarkCollectionId",
                principalTable: "BookmarkCollections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProductAffiliates_Products_ProductId",
                table: "OrderProductAffiliates",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCategories_Products_ProductId",
                table: "ProductCategories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductLikes_Products_ProductId",
                table: "ProductLikes",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOnboardingPreferences_Products_ProductId",
                table: "ProductOnboardingPreferences",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPairs_Products_ProductId",
                table: "ProductPairs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Brand_BrandId",
                table: "Products",
                column: "BrandId",
                principalTable: "Brand",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSimilars_Products_ProductId",
                table: "ProductSimilars",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
