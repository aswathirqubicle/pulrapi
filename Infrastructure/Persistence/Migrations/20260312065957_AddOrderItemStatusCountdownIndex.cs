using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemStatusCountdownIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_Status_Countdown",
                table: "OrderProductAffiliates",
                columns: new[] { "OrderItemStatus", "CountdownExpiryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_Status_Countdown",
                table: "OrderProductAffiliates");
        }
    }
}
