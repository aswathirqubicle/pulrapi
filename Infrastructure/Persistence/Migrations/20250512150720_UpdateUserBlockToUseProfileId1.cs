using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class UpdateUserBlockToUseProfileId1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBlocks_Profiles_BlockedProfileId",
                table: "UserBlocks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBlocks_Profiles_BlockerProfileId",
                table: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_Uid",
                table: "Profiles");

            migrationBuilder.AlterColumn<string>(
                name: "BlockerProfileId",
                table: "UserBlocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "BlockedProfileId",
                table: "UserBlocks",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Profiles_Uid",
                table: "Profiles",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Uid",
                table: "Profiles",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBlocks_Profiles_BlockedProfileId",
                table: "UserBlocks",
                column: "BlockedProfileId",
                principalTable: "Profiles",
                principalColumn: "Uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBlocks_Profiles_BlockerProfileId",
                table: "UserBlocks",
                column: "BlockerProfileId",
                principalTable: "Profiles",
                principalColumn: "Uid",
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

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Profiles_Uid",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_Uid",
                table: "Profiles");

            migrationBuilder.AlterColumn<int>(
                name: "BlockerProfileId",
                table: "UserBlocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "BlockedProfileId",
                table: "UserBlocks",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_Uid",
                table: "Profiles",
                column: "Uid");

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
    }
}
