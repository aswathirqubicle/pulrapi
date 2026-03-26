using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderProductIdtoWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderProductAffiliateId",
                table: "WalletTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_OrderProductAffiliateId",
                table: "WalletTransactions",
                column: "OrderProductAffiliateId");

            migrationBuilder.AddForeignKey(
                name: "FK_WalletTransactions_OrderProductAffiliates_OrderProductAffiliateId",
                table: "WalletTransactions",
                column: "OrderProductAffiliateId",
                principalTable: "OrderProductAffiliates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WalletTransactions_OrderProductAffiliates_OrderProductAffiliateId",
                table: "WalletTransactions");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_OrderProductAffiliateId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "OrderProductAffiliateId",
                table: "WalletTransactions");
        }
    }
}