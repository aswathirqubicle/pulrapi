using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSnapshotToOrderProductAffiliate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryImageUrlSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductDescriptionSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNameSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductPriceSnapshot",
                table: "OrderProductAffiliates",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryImageUrlSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductDescriptionSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductNameSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductPriceSnapshot",
                table: "OrderProductAffiliates");
        }
    }
}
