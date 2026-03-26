using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderProductAffiliatePrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderProductAffiliates",
                table: "OrderProductAffiliates");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderProductAffiliates",
                table: "OrderProductAffiliates",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_OrderId",
                table: "OrderProductAffiliates",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderProductAffiliates",
                table: "OrderProductAffiliates");

            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_OrderId",
                table: "OrderProductAffiliates");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderProductAffiliates",
                table: "OrderProductAffiliates",
                columns: new[] { "OrderId", "ProductId" });
        }
    }
}
