using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class UpdateUserBlockToUseProfileId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBlocks_AspNetUsers_BlockedId",
                table: "UserBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBlocks_AspNetUsers_BlockerId",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_BlockedId",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_BlockerId",
                table: "UserBlocks");

            migrationBuilder.DropColumn(
                name: "BlockedId",
                table: "UserBlocks");

            migrationBuilder.DropColumn(
                name: "BlockerId",
                table: "UserBlocks");

            migrationBuilder.AddColumn<int>(
                name: "BlockedProfileId",
                table: "UserBlocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BlockerProfileId",
                table: "UserBlocks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedProfileId",
                table: "UserBlocks",
                column: "BlockedProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerProfileId",
                table: "UserBlocks",
                column: "BlockerProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserBlocks_Profiles_BlockedProfileId",
                table: "UserBlocks",
                column: "BlockedProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBlocks_Profiles_BlockerProfileId",
                table: "UserBlocks",
                column: "BlockerProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBlocks_Profiles_BlockedProfileId",
                table: "UserBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBlocks_Profiles_BlockerProfileId",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_BlockedProfileId",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_UserBlocks_BlockerProfileId",
                table: "UserBlocks");

            migrationBuilder.DropColumn(
                name: "BlockedProfileId",
                table: "UserBlocks");

            migrationBuilder.DropColumn(
                name: "BlockerProfileId",
                table: "UserBlocks");

            migrationBuilder.AddColumn<string>(
                name: "BlockedId",
                table: "UserBlocks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlockerId",
                table: "UserBlocks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedId",
                table: "UserBlocks",
                column: "BlockedId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerId",
                table: "UserBlocks",
                column: "BlockerId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserBlocks_AspNetUsers_BlockedId",
                table: "UserBlocks",
                column: "BlockedId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBlocks_AspNetUsers_BlockerId",
                table: "UserBlocks",
                column: "BlockerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
