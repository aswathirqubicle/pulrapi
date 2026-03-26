using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateBookmarkCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookmarkCollectionId",
                table: "Bookmarks",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookmarkCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ProfileId = table.Column<int>(type: "integer", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookmarkCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookmarkCollections_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_BookmarkCollectionId",
                table: "Bookmarks",
                column: "BookmarkCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkCollections_ProfileId",
                table: "BookmarkCollections",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkCollections_Uid",
                table: "BookmarkCollections",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookmarks_BookmarkCollections_BookmarkCollectionId",
                table: "Bookmarks",
                column: "BookmarkCollectionId",
                principalTable: "BookmarkCollections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookmarks_BookmarkCollections_BookmarkCollectionId",
                table: "Bookmarks");

            migrationBuilder.DropTable(
                name: "BookmarkCollections");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_BookmarkCollectionId",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "BookmarkCollectionId",
                table: "Bookmarks");
        }
    }
}
