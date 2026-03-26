using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class StoreLikesStoryStore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_Stories_StoryId",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_StoryId",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "StoryId",
                table: "Profiles");

            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Stories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoryLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryId = table.Column<int>(type: "integer", nullable: false),
                    LikedById = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryLikes_Profiles_LikedById",
                        column: x => x.LikedById,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryLikes_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stories_StoreId",
                table: "Stories",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryLikes_LikedById",
                table: "StoryLikes",
                column: "LikedById");

            migrationBuilder.CreateIndex(
                name: "IX_StoryLikes_StoryId",
                table: "StoryLikes",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryLikes_Uid",
                table: "StoryLikes",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Stores_StoreId",
                table: "Stories",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Stores_StoreId",
                table: "Stories");

            migrationBuilder.DropTable(
                name: "StoryLikes");

            migrationBuilder.DropIndex(
                name: "IX_Stories_StoreId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Stories");

            migrationBuilder.AddColumn<int>(
                name: "StoryId",
                table: "Profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_StoryId",
                table: "Profiles",
                column: "StoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Stories_StoryId",
                table: "Profiles",
                column: "StoryId",
                principalTable: "Stories",
                principalColumn: "Id");
        }
    }
}
