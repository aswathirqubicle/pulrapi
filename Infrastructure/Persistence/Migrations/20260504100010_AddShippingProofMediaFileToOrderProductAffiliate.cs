using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingProofMediaFileToOrderProductAffiliate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShippingProofMediaFileId",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_ShippingProofMediaFileId",
                table: "OrderProductAffiliates",
                column: "ShippingProofMediaFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProductAffiliates_MediaFiles_ShippingProofMediaFileId",
                table: "OrderProductAffiliates",
                column: "ShippingProofMediaFileId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderProductAffiliates_MediaFiles_ShippingProofMediaFileId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_ShippingProofMediaFileId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ShippingProofMediaFileId",
                table: "OrderProductAffiliates");
        }
    }
}
