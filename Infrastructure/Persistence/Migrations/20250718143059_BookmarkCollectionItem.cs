using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BookmarkCollectionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookmarkCollections_Profiles_ProfileId",
                table: "BookmarkCollections");

            migrationBuilder.AlterColumn<int>(
                name: "ProfileId",
                table: "BookmarkCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "BookmarkCollectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookmarkId = table.Column<int>(type: "integer", nullable: false),
                    BookmarkCollectionId = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookmarkCollectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookmarkCollectionItems_BookmarkCollections_BookmarkCollect~",
                        column: x => x.BookmarkCollectionId,
                        principalTable: "BookmarkCollections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookmarkCollectionItems_Bookmarks_BookmarkId",
                        column: x => x.BookmarkId,
                        principalTable: "Bookmarks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkCollectionItems_BookmarkCollectionId",
                table: "BookmarkCollectionItems",
                column: "BookmarkCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkCollectionItems_BookmarkId",
                table: "BookmarkCollectionItems",
                column: "BookmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkCollectionItems_Uid",
                table: "BookmarkCollectionItems",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_BookmarkCollections_Profiles_ProfileId",
                table: "BookmarkCollections",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookmarkCollections_Profiles_ProfileId",
                table: "BookmarkCollections");

            migrationBuilder.DropTable(
                name: "BookmarkCollectionItems");

            migrationBuilder.AlterColumn<int>(
                name: "ProfileId",
                table: "BookmarkCollections",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_BookmarkCollections_Profiles_ProfileId",
                table: "BookmarkCollections",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id");
        }
    }
}
