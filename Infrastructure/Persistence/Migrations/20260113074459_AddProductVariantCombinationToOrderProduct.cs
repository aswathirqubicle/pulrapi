using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantCombinationToOrderProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductVariantCombinationId",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductVariantCombinationUidSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_ProductVariantCombinationId",
                table: "OrderProductAffiliates",
                column: "ProductVariantCombinationId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProductAffiliates_ProductVariantCombinations_ProductVa~",
                table: "OrderProductAffiliates",
                column: "ProductVariantCombinationId",
                principalTable: "ProductVariantCombinations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderProductAffiliates_ProductVariantCombinations_ProductVa~",
                table: "OrderProductAffiliates");

            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_ProductVariantCombinationId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductVariantCombinationId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductVariantCombinationUidSnapshot",
                table: "OrderProductAffiliates");
        }
    }
}
