using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeShippingDetailsUserIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingDetails_AspNetUsers_UserId",
                table: "ShippingDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingDetails_AspNetUsers_UserId",
                table: "ShippingDetails",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingDetails_AspNetUsers_UserId",
                table: "ShippingDetails");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingDetails_AspNetUsers_UserId",
                table: "ShippingDetails",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
