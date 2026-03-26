using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class AddProfileIdToStory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileId",
                table: "Stories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stories_ProfileId",
                table: "Stories",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Profiles_ProfileId",
                table: "Stories",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Uid",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Profiles_ProfileId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_ProfileId",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Stories");

        }
    }
}
