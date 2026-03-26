using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class TableRename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_Spots_SpotId",
                table: "Profiles");

            migrationBuilder.DropTable(
                name: "SpotHashTags");

            migrationBuilder.DropTable(
                name: "SpotProductTags");

            migrationBuilder.DropTable(
                name: "SpotProfileMentions");

            migrationBuilder.DropTable(
                name: "Spots");

            migrationBuilder.RenameColumn(
                name: "SpotId",
                table: "Profiles",
                newName: "StoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Profiles_SpotId",
                table: "Profiles",
                newName: "IX_Profiles_StoryId");

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "text", nullable: true),
                    StoryExpiresIn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UserId1 = table.Column<string>(type: "text", nullable: true),
                    MediaFileId = table.Column<int>(type: "integer", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stories_AspNetUsers_UserId1",
                        column: x => x.UserId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Stories_MediaFiles_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StoryHashTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryId = table.Column<int>(type: "integer", nullable: false),
                    HashTagId = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryHashTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryHashTags_Hashtags_HashTagId",
                        column: x => x.HashTagId,
                        principalTable: "Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryHashTags_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryProductTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryId = table.Column<int>(type: "integer", nullable: false),
                    AffiliateId = table.Column<string>(type: "text", nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    PositionLeftPercent = table.Column<double>(type: "double precision", nullable: false),
                    PositionTopPercent = table.Column<double>(type: "double precision", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryProductTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryProductTags_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryProductTags_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryProfileMentions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryId = table.Column<int>(type: "integer", nullable: false),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryProfileMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryProfileMentions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoryProfileMentions_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stories_MediaFileId",
                table: "Stories",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_Uid",
                table: "Stories",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_UserId1",
                table: "Stories",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_StoryHashTags_HashTagId",
                table: "StoryHashTags",
                column: "HashTagId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryHashTags_StoryId",
                table: "StoryHashTags",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryHashTags_Uid",
                table: "StoryHashTags",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_StoryProductTags_ProductId",
                table: "StoryProductTags",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryProductTags_StoryId",
                table: "StoryProductTags",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryProductTags_Uid",
                table: "StoryProductTags",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_StoryProfileMentions_ProfileId",
                table: "StoryProfileMentions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryProfileMentions_StoryId",
                table: "StoryProfileMentions",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryProfileMentions_Uid",
                table: "StoryProfileMentions",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Stories_StoryId",
                table: "Profiles",
                column: "StoryId",
                principalTable: "Stories",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_Stories_StoryId",
                table: "Profiles");

            migrationBuilder.DropTable(
                name: "StoryHashTags");

            migrationBuilder.DropTable(
                name: "StoryProductTags");

            migrationBuilder.DropTable(
                name: "StoryProfileMentions");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.RenameColumn(
                name: "StoryId",
                table: "Profiles",
                newName: "SpotId");

            migrationBuilder.RenameIndex(
                name: "IX_Profiles_StoryId",
                table: "Profiles",
                newName: "IX_Profiles_SpotId");

            migrationBuilder.CreateTable(
                name: "Spots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MediaFileId = table.Column<int>(type: "integer", nullable: true),
                    UserId1 = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    SpotExpiresIn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Spots_AspNetUsers_UserId1",
                        column: x => x.UserId1,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Spots_MediaFiles_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SpotHashTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HashTagId = table.Column<int>(type: "integer", nullable: false),
                    SpotId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotHashTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotHashTags_Hashtags_HashTagId",
                        column: x => x.HashTagId,
                        principalTable: "Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpotHashTags_Spots_SpotId",
                        column: x => x.SpotId,
                        principalTable: "Spots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpotProductTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SpotId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    PositionLeftPercent = table.Column<double>(type: "double precision", nullable: false),
                    PositionTopPercent = table.Column<double>(type: "double precision", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotProductTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotProductTags_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpotProductTags_Spots_SpotId",
                        column: x => x.SpotId,
                        principalTable: "Spots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpotProfileMentions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false),
                    SpotId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotProfileMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpotProfileMentions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpotProfileMentions_Spots_SpotId",
                        column: x => x.SpotId,
                        principalTable: "Spots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpotHashTags_HashTagId",
                table: "SpotHashTags",
                column: "HashTagId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotHashTags_SpotId",
                table: "SpotHashTags",
                column: "SpotId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotHashTags_Uid",
                table: "SpotHashTags",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_SpotProductTags_ProductId",
                table: "SpotProductTags",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotProductTags_SpotId",
                table: "SpotProductTags",
                column: "SpotId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotProductTags_Uid",
                table: "SpotProductTags",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_SpotProfileMentions_ProfileId",
                table: "SpotProfileMentions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotProfileMentions_SpotId",
                table: "SpotProfileMentions",
                column: "SpotId");

            migrationBuilder.CreateIndex(
                name: "IX_SpotProfileMentions_Uid",
                table: "SpotProfileMentions",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_Spots_MediaFileId",
                table: "Spots",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Spots_Uid",
                table: "Spots",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_Spots_UserId1",
                table: "Spots",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Spots_SpotId",
                table: "Profiles",
                column: "SpotId",
                principalTable: "Spots",
                principalColumn: "Id");
        }
    }
}
