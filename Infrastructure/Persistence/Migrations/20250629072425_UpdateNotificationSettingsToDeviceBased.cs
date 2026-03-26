using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationSettingsToDeviceBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "UserNotificationSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PushToken",
                table: "UserNotificationSettings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "UserNotificationSettings");

            migrationBuilder.DropColumn(
                name: "PushToken",
                table: "UserNotificationSettings");
        }
    }
}
