using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscrowWalletAndRefundSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckinPromptSentAt",
                table: "OrderProductAffiliates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ColabInviteId",
                table: "OrderProductAffiliates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatorUserId",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveredBy",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryProofUrl",
                table: "OrderProductAffiliates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EscrowReleaseAt",
                table: "OrderProductAffiliates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EscrowStatus",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExchangeEligibleUntil",
                table: "OrderProductAffiliates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundEligibleUntil",
                table: "OrderProductAffiliates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EscrowWallets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    LockedBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    CurrencyId = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscrowWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscrowWallets_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscrowWallets_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EscrowWalletTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EscrowWalletId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    OrderProductAffiliateId = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellerAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CreatorAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PlatformAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsCollabSale = table.Column<bool>(type: "boolean", nullable: false),
                    CreatorUserId = table.Column<string>(type: "text", nullable: true),
                    EscrowReleaseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StripeTransferIdSeller = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeTransferIdCreator = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeRefundId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscrowWalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscrowWalletTransactions_EscrowWallets_EscrowWalletId",
                        column: x => x.EscrowWalletId,
                        principalTable: "EscrowWallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscrowWalletTransactions_OrderProductAffiliates_OrderProduc~",
                        column: x => x.OrderProductAffiliateId,
                        principalTable: "OrderProductAffiliates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EscrowWalletTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EscrowWallets_CurrencyId",
                table: "EscrowWallets",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowWallets_ProfileId",
                table: "EscrowWallets",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowWallets_Uid",
                table: "EscrowWallets",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowWalletTransactions_EscrowWalletId",
                table: "EscrowWalletTransactions",
                column: "EscrowWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowWalletTransactions_OrderId",
                table: "EscrowWalletTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowWalletTransactions_OrderProductAffiliateId",
                table: "EscrowWalletTransactions",
                column: "OrderProductAffiliateId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowWalletTransactions_StripePaymentIntentId",
                table: "EscrowWalletTransactions",
                column: "StripePaymentIntentId");

            migrationBuilder.CreateIndex(
                name: "IX_EscrowWalletTransactions_Uid",
                table: "EscrowWalletTransactions",
                column: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EscrowWalletTransactions");

            migrationBuilder.DropTable(
                name: "EscrowWallets");

            migrationBuilder.DropColumn(
                name: "CheckinPromptSentAt",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ColabInviteId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "CreatorUserId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "DeliveredBy",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "DeliveryProofUrl",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "EscrowReleaseAt",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "EscrowStatus",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ExchangeEligibleUntil",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "RefundEligibleUntil",
                table: "OrderProductAffiliates");
        }
    }
}
