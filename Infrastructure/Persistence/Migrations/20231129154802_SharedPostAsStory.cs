using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class SharedPostAsStory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SharedPostId",
                table: "Stories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stories_SharedPostId",
                table: "Stories",
                column: "SharedPostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Posts_SharedPostId",
                table: "Stories",
                column: "SharedPostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Posts_SharedPostId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_SharedPostId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "SharedPostId",
                table: "Stories");
        }
    }
}
