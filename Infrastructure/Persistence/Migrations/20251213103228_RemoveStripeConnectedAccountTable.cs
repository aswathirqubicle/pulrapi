using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStripeConnectedAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_StripeConnectedAccounts_StripeConnectedAccountId",
                table: "Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Stores_StripeConnectedAccounts_StripeConnectedAccountId",
                table: "Stores");

            migrationBuilder.DropTable(
                name: "StripeConnectedAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Stores_StripeConnectedAccountId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_StripeConnectedAccountId",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "StripeConnectedAccountId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "StripeConnectedAccountId",
                table: "Profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StripeConnectedAccountId",
                table: "Stores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StripeConnectedAccountId",
                table: "Profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StripeConnectedAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<string>(type: "text", nullable: true),
                    AccountTermsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true),
                    StripeAccountResponseJson = table.Column<string>(type: "text", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StripeConnectedAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stores_StripeConnectedAccountId",
                table: "Stores",
                column: "StripeConnectedAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_StripeConnectedAccountId",
                table: "Profiles",
                column: "StripeConnectedAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StripeConnectedAccounts_Uid",
                table: "StripeConnectedAccounts",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_StripeConnectedAccounts_StripeConnectedAccountId",
                table: "Profiles",
                column: "StripeConnectedAccountId",
                principalTable: "StripeConnectedAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stores_StripeConnectedAccounts_StripeConnectedAccountId",
                table: "Stores",
                column: "StripeConnectedAccountId",
                principalTable: "StripeConnectedAccounts",
                principalColumn: "Id");
        }
    }
}
