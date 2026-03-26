using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateCommunicationmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommunicationMail",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "SellerSettings",
                newName: "CommunicationMail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CommunicationMail",
                table: "SellerSettings",
                newName: "Email");

            migrationBuilder.AddColumn<string>(
                name: "CommunicationMail",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }
    }
}
