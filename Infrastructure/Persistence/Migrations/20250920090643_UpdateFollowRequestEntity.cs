using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFollowRequestEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_MediaFiles_MediaFileId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Countries_CountryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_CountryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Products");

            migrationBuilder.AlterColumn<int>(
                name: "MediaFileId",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Countries_Uid",
                table: "Countries",
                column: "Uid");

            migrationBuilder.CreateTable(
                name: "FollowRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequesterProfileId = table.Column<string>(type: "text", nullable: true),
                    TargetProfileId = table.Column<string>(type: "text", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CountryUid",
                table: "Products",
                column: "CountryUid");

            migrationBuilder.CreateIndex(
                name: "IX_FollowRequests_Uid",
                table: "FollowRequests",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_MediaFiles_MediaFileId",
                table: "Posts",
                column: "MediaFileId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Clean up orphaned CountryUid values before creating foreign key constraint
            migrationBuilder.Sql(@"
                UPDATE ""Products"" 
                SET ""CountryUid"" = NULL 
                WHERE ""CountryUid"" IS NOT NULL 
                AND ""CountryUid"" NOT IN (SELECT ""Uid"" FROM ""Countries"")
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Countries_CountryUid",
                table: "Products",
                column: "CountryUid",
                principalTable: "Countries",
                principalColumn: "Uid",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_MediaFiles_MediaFileId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Countries_CountryUid",
                table: "Products");

            migrationBuilder.DropTable(
                name: "FollowRequests");

            migrationBuilder.DropIndex(
                name: "IX_Products_CountryUid",
                table: "Products");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Countries_Uid",
                table: "Countries");

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MediaFileId",
                table: "Posts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CountryId",
                table: "Products",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_MediaFiles_MediaFileId",
                table: "Posts",
                column: "MediaFileId",
                principalTable: "MediaFiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Countries_CountryId",
                table: "Products",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");
        }
    }
}
