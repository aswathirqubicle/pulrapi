using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHlsFieldsToMediaFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvailableQualities",
                table: "MediaFiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HlsBasePath",
                table: "MediaFiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHlsProcessed",
                table: "MediaFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalUrl",
                table: "MediaFiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoDurationSeconds",
                table: "MediaFiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableQualities",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "HlsBasePath",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "IsHlsProcessed",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "OriginalUrl",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "VideoDurationSeconds",
                table: "MediaFiles");
        }
    }
}
