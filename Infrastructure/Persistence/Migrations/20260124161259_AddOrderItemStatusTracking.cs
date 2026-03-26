using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemStatusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "OrderProductAffiliates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderItemStatus",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippedAt",
                table: "OrderProductAffiliates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingProvider",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "OrderItemStatus",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ShippingProvider",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "OrderProductAffiliates");
        }
    }
}
