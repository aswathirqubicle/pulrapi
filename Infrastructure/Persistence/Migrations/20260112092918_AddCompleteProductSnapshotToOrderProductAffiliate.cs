using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompleteProductSnapshotToOrderProductAffiliate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCodeSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCodeSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryTimeSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductBrandSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ProductMaxPriceSnapshot",
                table: "OrderProductAffiliates",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ProductMinPriceSnapshot",
                table: "OrderProductAffiliates",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductTypeSnapshot",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProfileUidSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileUsernameSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCostSnapshot",
                table: "OrderProductAffiliates",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantTypesSnapshot",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCodeSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "CurrencyCodeSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "DeliveryTimeSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductBrandSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductMaxPriceSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductMinPriceSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProductTypeSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProfileUidSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ProfileUsernameSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ShippingCostSnapshot",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "VariantTypesSnapshot",
                table: "OrderProductAffiliates");
        }
    }
}
