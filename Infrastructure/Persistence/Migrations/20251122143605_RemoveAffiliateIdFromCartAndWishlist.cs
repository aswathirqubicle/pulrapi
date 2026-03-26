using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAffiliateIdFromCartAndWishlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBagProducts_Affiliates_AffiliateId",
                table: "UserBagProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_UserWishlistProducts_Affiliates_AffiliateId",
                table: "UserWishlistProducts");

            migrationBuilder.DropIndex(
                name: "IX_UserWishlistProducts_AffiliateId",
                table: "UserWishlistProducts");

            migrationBuilder.DropIndex(
                name: "IX_UserBagProducts_AffiliateId",
                table: "UserBagProducts");

            migrationBuilder.DropColumn(
                name: "AffiliateId",
                table: "UserWishlistProducts");

            migrationBuilder.DropColumn(
                name: "AffiliateId",
                table: "UserBagProducts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AffiliateId",
                table: "UserWishlistProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AffiliateId",
                table: "UserBagProducts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWishlistProducts_AffiliateId",
                table: "UserWishlistProducts",
                column: "AffiliateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBagProducts_AffiliateId",
                table: "UserBagProducts",
                column: "AffiliateId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserBagProducts_Affiliates_AffiliateId",
                table: "UserBagProducts",
                column: "AffiliateId",
                principalTable: "Affiliates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserWishlistProducts_Affiliates_AffiliateId",
                table: "UserWishlistProducts",
                column: "AffiliateId",
                principalTable: "Affiliates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
