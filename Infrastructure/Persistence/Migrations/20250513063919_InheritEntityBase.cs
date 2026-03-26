using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class InheritEntityBase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_AspNetUsers_UserId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Posts_SharedPostId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Profiles_ProfileId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Stores_StoreId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_ProfileId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_Uid",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Stories");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProfileFollowers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProfileFollowers",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ProfileFollowers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "ProfileFollowers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "ProfileFollowers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProfileFollowers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PostMyStyles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PostMyStyles",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PostMyStyles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "PostMyStyles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "PostMyStyles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PostMyStyles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PostLikes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PostLikes",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PostLikes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "PostLikes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Uid",
                table: "PostLikes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PostLikes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stories_Uid",
                table: "Stories",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileFollowers_Uid",
                table: "ProfileFollowers",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_PostMyStyles_Uid",
                table: "PostMyStyles",
                column: "Uid");

            migrationBuilder.CreateIndex(
                name: "IX_PostLikes_Uid",
                table: "PostLikes",
                column: "Uid");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_AspNetUsers_UserId",
                table: "Stories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Posts_SharedPostId",
                table: "Stories",
                column: "SharedPostId",
                principalTable: "Posts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Stores_StoreId",
                table: "Stories",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stories_AspNetUsers_UserId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Posts_SharedPostId",
                table: "Stories");

            migrationBuilder.DropForeignKey(
                name: "FK_Stories_Stores_StoreId",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Stories_Uid",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_ProfileFollowers_Uid",
                table: "ProfileFollowers");

            migrationBuilder.DropIndex(
                name: "IX_PostMyStyles_Uid",
                table: "PostMyStyles");

            migrationBuilder.DropIndex(
                name: "IX_PostLikes_Uid",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProfileFollowers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProfileFollowers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ProfileFollowers");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "ProfileFollowers");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProfileFollowers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProfileFollowers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PostMyStyles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PostMyStyles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PostMyStyles");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "PostMyStyles");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "PostMyStyles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PostMyStyles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "PostLikes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PostLikes");

            migrationBuilder.AddColumn<string>(
                name: "ProfileId",
                table: "Stories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stories_ProfileId",
                table: "Stories",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_Uid",
                table: "Stories",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_AspNetUsers_UserId",
                table: "Stories",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Posts_SharedPostId",
                table: "Stories",
                column: "SharedPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Profiles_ProfileId",
                table: "Stories",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Uid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stories_Stores_StoreId",
                table: "Stories",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
