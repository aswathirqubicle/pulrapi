using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantCombinationsWithSKU : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "ProductVariantOptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ProductVariantCombinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    SKU = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    StockLevel = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantCombinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantCombinations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantCombinationOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductVariantCombinationId = table.Column<int>(type: "integer", nullable: false),
                    ProductVariantOptionId = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantCombinationOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantCombinationOptions_ProductVariantCombinations~",
                        column: x => x.ProductVariantCombinationId,
                        principalTable: "ProductVariantCombinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariantCombinationOptions_ProductVariantOptions_Prod~",
                        column: x => x.ProductVariantOptionId,
                        principalTable: "ProductVariantOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantCombinationOptions_ProductVariantCombinationI~",
                table: "ProductVariantCombinationOptions",
                columns: new[] { "ProductVariantCombinationId", "ProductVariantOptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantCombinationOptions_ProductVariantOptionId",
                table: "ProductVariantCombinationOptions",
                column: "ProductVariantOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantCombinationOptions_Uid",
                table: "ProductVariantCombinationOptions",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantCombinations_ProductId",
                table: "ProductVariantCombinations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantCombinations_SKU",
                table: "ProductVariantCombinations",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantCombinations_Uid",
                table: "ProductVariantCombinations",
                column: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariantCombinationOptions");

            migrationBuilder.DropTable(
                name: "ProductVariantCombinations");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "ProductVariantOptions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
