using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryUidToShippingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryUid",
                table: "ShippingDetails",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingDetails_CountryUid",
                table: "ShippingDetails",
                column: "CountryUid");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingDetails_Countries_CountryUid",
                table: "ShippingDetails",
                column: "CountryUid",
                principalTable: "Countries",
                principalColumn: "Uid",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingDetails_Countries_CountryUid",
                table: "ShippingDetails");

            migrationBuilder.DropIndex(
                name: "IX_ShippingDetails_CountryUid",
                table: "ShippingDetails");

            migrationBuilder.DropColumn(
                name: "CountryUid",
                table: "ShippingDetails");
        }
    }
}
