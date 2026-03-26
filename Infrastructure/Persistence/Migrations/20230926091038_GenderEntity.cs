using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class GenderEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "OnboardingPreferences");

            migrationBuilder.AddColumn<int>(
                name: "GenderId",
                table: "Profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GenderId",
                table: "OnboardingPreferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Genders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_GenderId",
                table: "Profiles",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingPreferences_GenderId",
                table: "OnboardingPreferences",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Genders_Uid",
                table: "Genders",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_OnboardingPreferences_Genders_GenderId",
                table: "OnboardingPreferences",
                column: "GenderId",
                principalTable: "Genders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Genders_GenderId",
                table: "Profiles",
                column: "GenderId",
                principalTable: "Genders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OnboardingPreferences_Genders_GenderId",
                table: "OnboardingPreferences");

            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_Genders_GenderId",
                table: "Profiles");

            migrationBuilder.DropTable(
                name: "Genders");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_GenderId",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingPreferences_GenderId",
                table: "OnboardingPreferences");

            migrationBuilder.DropColumn(
                name: "GenderId",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "GenderId",
                table: "OnboardingPreferences");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "OnboardingPreferences",
                type: "integer",
                nullable: true);
        }
    }
}
