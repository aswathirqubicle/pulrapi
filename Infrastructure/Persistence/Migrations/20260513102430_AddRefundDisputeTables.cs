using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundDisputeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RefundResponseDays",
                table: "PlatformSettings",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.CreateTable(
                name: "RefundDisputes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderProductAffiliateId = table.Column<int>(type: "integer", nullable: false),
                    SellerProfileId = table.Column<int>(type: "integer", nullable: true),
                    BuyerProfileId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SellerRejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SellerRejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AdminResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByAdminUserId = table.Column<string>(type: "text", nullable: true),
                    Uid = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundDisputes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundDisputes_OrderProductAffiliates_OrderProductAffiliate~",
                        column: x => x.OrderProductAffiliateId,
                        principalTable: "OrderProductAffiliates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundDisputes_Profiles_BuyerProfileId",
                        column: x => x.BuyerProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundDisputes_Profiles_SellerProfileId",
                        column: x => x.SellerProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefundDisputeEvidences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RefundDisputeId = table.Column<int>(type: "integer", nullable: false),
                    MediaFileId = table.Column<int>(type: "integer", nullable: false),
                    EvidenceType = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_RefundDisputeEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundDisputeEvidences_MediaFiles_MediaFileId",
                        column: x => x.MediaFileId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundDisputeEvidences_RefundDisputes_RefundDisputeId",
                        column: x => x.RefundDisputeId,
                        principalTable: "RefundDisputes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundDisputeEvidences_MediaFileId",
                table: "RefundDisputeEvidences",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundDisputeEvidences_RefundDisputeId",
                table: "RefundDisputeEvidences",
                column: "RefundDisputeId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundDisputeEvidences_Uid",
                table: "RefundDisputeEvidences",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_RefundDisputes_BuyerProfileId",
                table: "RefundDisputes",
                column: "BuyerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundDisputes_OrderProductAffiliateId",
                table: "RefundDisputes",
                column: "OrderProductAffiliateId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundDisputes_SellerProfileId",
                table: "RefundDisputes",
                column: "SellerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundDisputes_Status",
                table: "RefundDisputes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RefundDisputes_Uid",
                table: "RefundDisputes",
                column: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundDisputeEvidences");

            migrationBuilder.DropTable(
                name: "RefundDisputes");

            migrationBuilder.DropColumn(
                name: "RefundResponseDays",
                table: "PlatformSettings");
        }
    }
}
