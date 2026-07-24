using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBuyerRefundReasonAndReturnAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuyerRefundReason",
                table: "RefundDisputes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BuyerRefundRequestedAt",
                table: "RefundDisputes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnAddressLine1",
                table: "RefundDisputes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnAddressLine2",
                table: "RefundDisputes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCity",
                table: "RefundDisputes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCountry",
                table: "RefundDisputes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnFullName",
                table: "RefundDisputes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnPhone",
                table: "RefundDisputes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnPostalCode",
                table: "RefundDisputes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnState",
                table: "RefundDisputes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerRefundReason",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "BuyerRefundRequestedAt",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "ReturnAddressLine1",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "ReturnAddressLine2",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "ReturnCity",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "ReturnCountry",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "ReturnFullName",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "ReturnPhone",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "ReturnPostalCode",
                table: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "ReturnState",
                table: "RefundDisputes");

        }
    }
}
