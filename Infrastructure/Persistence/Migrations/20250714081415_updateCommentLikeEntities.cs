using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateCommentLikeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CommentLikes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "CommentLikes",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CommentLikes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "CommentLikes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "CommentLikes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CommentLikes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_Uid",
                table: "CommentLikes",
                column: "Uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommentLikes_Uid",
                table: "CommentLikes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CommentLikes");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "CommentLikes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CommentLikes");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "CommentLikes");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "CommentLikes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CommentLikes");
        }
    }
}
