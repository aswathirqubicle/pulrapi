using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationHistoryRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "UserPushTokens",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Activities",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistories_ActorUserId",
                table: "NotificationHistories",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistories_ReceiverUserId",
                table: "NotificationHistories",
                column: "ReceiverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistories_Profiles_ActorUserId",
                table: "NotificationHistories",
                column: "ActorUserId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationHistories_Profiles_ReceiverUserId",
                table: "NotificationHistories",
                column: "ReceiverUserId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistories_Profiles_ActorUserId",
                table: "NotificationHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationHistories_Profiles_ReceiverUserId",
                table: "NotificationHistories");

            migrationBuilder.DropIndex(
                name: "IX_NotificationHistories_ActorUserId",
                table: "NotificationHistories");

            migrationBuilder.DropIndex(
                name: "IX_NotificationHistories_ReceiverUserId",
                table: "NotificationHistories");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserPushTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Activities",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
