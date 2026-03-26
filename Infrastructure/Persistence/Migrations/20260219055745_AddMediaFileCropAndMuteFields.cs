using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFileCropAndMuteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CropHeight",
                table: "MediaFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CropWidth",
                table: "MediaFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CropX",
                table: "MediaFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CropY",
                table: "MediaFiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMuted",
                table: "MediaFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CropHeight",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "CropWidth",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "CropX",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "CropY",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "IsMuted",
                table: "MediaFiles");
        }
    }
}
