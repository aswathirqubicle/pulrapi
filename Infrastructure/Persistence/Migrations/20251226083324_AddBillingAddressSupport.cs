using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAddressSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingDetails_ShippingDetailsId",
                table: "Orders");

            migrationBuilder.AddColumn<bool>(
                name: "IsBillingAddress",
                table: "ShippingDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BillingDetailsId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BillingDetailsId",
                table: "Orders",
                column: "BillingDetailsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingDetails_BillingDetailsId",
                table: "Orders",
                column: "BillingDetailsId",
                principalTable: "ShippingDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingDetails_ShippingDetailsId",
                table: "Orders",
                column: "ShippingDetailsId",
                principalTable: "ShippingDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingDetails_BillingDetailsId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ShippingDetails_ShippingDetailsId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BillingDetailsId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsBillingAddress",
                table: "ShippingDetails");

            migrationBuilder.DropColumn(
                name: "BillingDetailsId",
                table: "Orders");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ShippingDetails_ShippingDetailsId",
                table: "Orders",
                column: "ShippingDetailsId",
                principalTable: "ShippingDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
