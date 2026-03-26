using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderProductAffiliateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys first
            migrationBuilder.DropForeignKey(
                name: "FK_OrderProductAffiliates_Products_ProductId",
                table: "OrderProductAffiliates");

            // Drop Affiliate foreign key if it exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.table_constraints 
                        WHERE constraint_name = 'FK_OrderProductAffiliates_Affiliates_AffiliateId' 
                        AND table_name = 'OrderProductAffiliates'
                    ) THEN
                        ALTER TABLE ""OrderProductAffiliates"" DROP CONSTRAINT ""FK_OrderProductAffiliates_Affiliates_AffiliateId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderProductAffiliates",
                table: "OrderProductAffiliates");

            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_OrderId",
                table: "OrderProductAffiliates");

            migrationBuilder.AlterColumn<int>(
                name: "AffiliateId",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderProductAffiliates",
                table: "OrderProductAffiliates",
                columns: new[] { "OrderId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_AffiliateId",
                table: "OrderProductAffiliates",
                column: "AffiliateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProductAffiliates_Products_ProductId",
                table: "OrderProductAffiliates",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProductAffiliates_Affiliates_AffiliateId",
                table: "OrderProductAffiliates",
                column: "AffiliateId",
                principalTable: "Affiliates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderProductAffiliates_Products_ProductId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderProductAffiliates",
                table: "OrderProductAffiliates");

            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_AffiliateId",
                table: "OrderProductAffiliates");

            migrationBuilder.AlterColumn<int>(
                name: "AffiliateId",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderProductAffiliates",
                table: "OrderProductAffiliates",
                column: "AffiliateId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_OrderId",
                table: "OrderProductAffiliates",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProductAffiliates_Products_ProductId",
                table: "OrderProductAffiliates",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropForeignKey(
                name: "FK_OrderProductAffiliates_Affiliates_AffiliateId",
                table: "OrderProductAffiliates");
        }
    }
}
