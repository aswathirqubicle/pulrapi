using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderItemShippingProofsNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderProductAffiliates_MediaFiles_ShippingProofMediaFileId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropIndex(
                name: "IX_OrderProductAffiliates_ShippingProofMediaFileId",
                table: "OrderProductAffiliates");

            migrationBuilder.DropColumn(
                name: "ShippingProofMediaFileId",
                table: "OrderProductAffiliates");

            migrationBuilder.CreateTable(
                name: "OrderItemShippingProofs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderProductAffiliateId = table.Column<int>(type: "integer", nullable: false),
                    MediaFileId = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemShippingProofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemShippingProofs_MediaFiles_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItemShippingProofs_OrderProductAffiliates_OrderProduct~",
                        column: x => x.OrderProductAffiliateId,
                        principalTable: "OrderProductAffiliates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemShippingProofs_MediaFileId",
                table: "OrderItemShippingProofs",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemShippingProofs_OrderProductAffiliateId",
                table: "OrderItemShippingProofs",
                column: "OrderProductAffiliateId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemShippingProofs_Uid",
                table: "OrderItemShippingProofs",
                column: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemShippingProofs");

            migrationBuilder.AddColumn<int>(
                name: "ShippingProofMediaFileId",
                table: "OrderProductAffiliates",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderProductAffiliates_ShippingProofMediaFileId",
                table: "OrderProductAffiliates",
                column: "ShippingProofMediaFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderProductAffiliates_MediaFiles_ShippingProofMediaFileId",
                table: "OrderProductAffiliates",
                column: "ShippingProofMediaFileId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
