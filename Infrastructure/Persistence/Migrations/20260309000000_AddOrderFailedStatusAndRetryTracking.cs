using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFailedStatusAndRetryTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CountdownExpiryDate",
                table: "OrderProductAffiliates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NewCountdownExpiryDate",
                table: "OrderProductAffiliates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetryAllowed",
                table: "OrderProductAffiliates",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "CountdownExpiryDate",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "NewCountdownExpiryDate",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "IsRetryAllowed",
                table: "OrderProductAffiliates");
        }
    }
}