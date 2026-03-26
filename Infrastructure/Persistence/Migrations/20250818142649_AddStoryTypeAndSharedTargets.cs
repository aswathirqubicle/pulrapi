using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoryTypeAndSharedTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SharedCollectionId",
                table: "Stories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SharedProductId",
                table: "Stories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoryType",
                table: "Stories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Stories_SharedCollectionId",
                table: "Stories",
                column: "SharedCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_SharedProductId",
                table: "Stories",
                column: "SharedProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_BookmarkCollections_SharedCollectionId",
                table: "Stories",
                column: "SharedCollectionId",
                principalTable: "BookmarkCollections",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Products_SharedProductId",
                table: "Stories",
                column: "SharedProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_BookmarkCollections_SharedCollectionId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Products_SharedProductId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_SharedCollectionId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_SharedProductId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "SharedCollectionId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "SharedProductId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "StoryType",
                table: "Stories");
        }
    }
}
