using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookmarkCollectionItemToReferencePost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookmarkCollectionItems_Bookmarks_BookmarkId",
                table: "BookmarkCollectionItems");

            migrationBuilder.RenameColumn(
                name: "BookmarkId",
                table: "BookmarkCollectionItems",
                newName: "PostId");

            migrationBuilder.RenameIndex(
                name: "IX_BookmarkCollectionItems_BookmarkId",
                table: "BookmarkCollectionItems",
                newName: "IX_BookmarkCollectionItems_PostId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookmarkCollectionItems_Posts_PostId",
                table: "BookmarkCollectionItems",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookmarkCollectionItems_Posts_PostId",
                table: "BookmarkCollectionItems");

            migrationBuilder.RenameColumn(
                name: "PostId",
                table: "BookmarkCollectionItems",
                newName: "BookmarkId");

            migrationBuilder.RenameIndex(
                name: "IX_BookmarkCollectionItems_PostId",
                table: "BookmarkCollectionItems",
                newName: "IX_BookmarkCollectionItems_BookmarkId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookmarkCollectionItems_Bookmarks_BookmarkId",
                table: "BookmarkCollectionItems",
                column: "BookmarkId",
                principalTable: "Bookmarks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
