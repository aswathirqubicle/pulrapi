using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class ProductOnboardingPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductOnboardingPreferences",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    OnboardingPreferenceId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOnboardingPreferences", x => new { x.ProductId, x.OnboardingPreferenceId });
                    table.ForeignKey(
                        name: "FK_ProductOnboardingPreferences_OnboardingPreferences_Onboardi~",
                        column: x => x.OnboardingPreferenceId,
                        principalTable: "OnboardingPreferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductOnboardingPreferences_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOnboardingPreferences_OnboardingPreferenceId",
                table: "ProductOnboardingPreferences",
                column: "OnboardingPreferenceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductOnboardingPreferences");
        }
    }
}
