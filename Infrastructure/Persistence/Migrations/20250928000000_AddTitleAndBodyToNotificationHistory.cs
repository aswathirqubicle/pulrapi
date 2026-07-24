using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTitleAndBodyToNotificationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "NotificationHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "NotificationHistories",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "NotificationHistories");

            migrationBuilder.DropColumn(
                name: "Body",
                table: "NotificationHistories");
        }
    }
}
