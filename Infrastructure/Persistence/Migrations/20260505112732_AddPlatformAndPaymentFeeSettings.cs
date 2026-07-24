using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformAndPaymentFeeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StripeFeeAmount",
                table: "Orders",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePaymentIntentId",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentFeeSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrencyId = table.Column<int>(type: "integer", nullable: false),
                    FeePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    FixedFee = table.Column<decimal>(type: "numeric", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentFeeSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentFeeSettings_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommissionRate = table.Column<decimal>(type: "numeric", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric", nullable: false),
                    PlatformFeePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    DirectSaleSellerPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    CollabSaleSellerPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    CollabSaleCreatorPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumWithdrawalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DeliveryExtensionHours = table.Column<int>(type: "integer", nullable: false),
                    RefundWindowDays = table.Column<int>(type: "integer", nullable: false),
                    ExchangeWindowDays = table.Column<int>(type: "integer", nullable: false),
                    EscrowHoldDays = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFeeSettings_CurrencyId",
                table: "PaymentFeeSettings",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFeeSettings_Uid",
                table: "PaymentFeeSettings",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformSettings_Uid",
                table: "PlatformSettings",
                column: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentFeeSettings");

            migrationBuilder.DropTable(
                name: "PlatformSettings");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripeFeeAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StripePaymentIntentId",
                table: "Orders");
        }
    }
}
