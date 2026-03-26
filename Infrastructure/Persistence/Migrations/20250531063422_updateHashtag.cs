using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class updateHashtag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Hashtags",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Hashtags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Hashtags",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "Hashtags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "Hashtags",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Hashtags",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hashtags_Uid",
                table: "Hashtags",
                column: "Uid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hashtags_Uid",
                table: "Hashtags");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Hashtags");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Hashtags");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Hashtags");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Hashtags");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "Hashtags");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Hashtags");
        }
    }
}
