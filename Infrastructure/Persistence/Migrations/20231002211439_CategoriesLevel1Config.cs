using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class CategoriesLevel1Config : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubCategoryLevel2_Products_ProductId",
                table: "ProductSubCategoryLevel2");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubCategoryLevel2_SubCategoryLevel2_SubCategoryLevel~",
                table: "ProductSubCategoryLevel2");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategoryLevel1_Categories_CategoryId",
                table: "SubCategoryLevel1");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategoryLevel2_SubCategoryLevel1_SubCategoryLevel1Id",
                table: "SubCategoryLevel2");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubCategoryLevel2",
                table: "SubCategoryLevel2");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubCategoryLevel1",
                table: "SubCategoryLevel1");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductSubCategoryLevel2",
                table: "ProductSubCategoryLevel2");

            migrationBuilder.RenameTable(
                name: "SubCategoryLevel2",
                newName: "SubCategoryLevel2s");

            migrationBuilder.RenameTable(
                name: "SubCategoryLevel1",
                newName: "SubCategoryLevel1s");

            migrationBuilder.RenameTable(
                name: "ProductSubCategoryLevel2",
                newName: "ProductSubCategoryLevel2s");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategoryLevel2_Uid",
                table: "SubCategoryLevel2s",
                newName: "IX_SubCategoryLevel2s_Uid");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategoryLevel2_SubCategoryLevel1Id",
                table: "SubCategoryLevel2s",
                newName: "IX_SubCategoryLevel2s_SubCategoryLevel1Id");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategoryLevel1_Uid",
                table: "SubCategoryLevel1s",
                newName: "IX_SubCategoryLevel1s_Uid");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategoryLevel1_CategoryId",
                table: "SubCategoryLevel1s",
                newName: "IX_SubCategoryLevel1s_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSubCategoryLevel2_Uid",
                table: "ProductSubCategoryLevel2s",
                newName: "IX_ProductSubCategoryLevel2s_Uid");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSubCategoryLevel2_SubCategoryLevel2Id",
                table: "ProductSubCategoryLevel2s",
                newName: "IX_ProductSubCategoryLevel2s_SubCategoryLevel2Id");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSubCategoryLevel2_ProductId",
                table: "ProductSubCategoryLevel2s",
                newName: "IX_ProductSubCategoryLevel2s_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubCategoryLevel2s",
                table: "SubCategoryLevel2s",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubCategoryLevel1s",
                table: "SubCategoryLevel1s",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductSubCategoryLevel2s",
                table: "ProductSubCategoryLevel2s",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubCategoryLevel2s_Products_ProductId",
                table: "ProductSubCategoryLevel2s",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubCategoryLevel2s_SubCategoryLevel2s_SubCategoryLev~",
                table: "ProductSubCategoryLevel2s",
                column: "SubCategoryLevel2Id",
                principalTable: "SubCategoryLevel2s",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategoryLevel1s_Categories_CategoryId",
                table: "SubCategoryLevel1s",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategoryLevel2s_SubCategoryLevel1s_SubCategoryLevel1Id",
                table: "SubCategoryLevel2s",
                column: "SubCategoryLevel1Id",
                principalTable: "SubCategoryLevel1s",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubCategoryLevel2s_Products_ProductId",
                table: "ProductSubCategoryLevel2s");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductSubCategoryLevel2s_SubCategoryLevel2s_SubCategoryLev~",
                table: "ProductSubCategoryLevel2s");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategoryLevel1s_Categories_CategoryId",
                table: "SubCategoryLevel1s");

            migrationBuilder.DropForeignKey(
                name: "FK_SubCategoryLevel2s_SubCategoryLevel1s_SubCategoryLevel1Id",
                table: "SubCategoryLevel2s");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubCategoryLevel2s",
                table: "SubCategoryLevel2s");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubCategoryLevel1s",
                table: "SubCategoryLevel1s");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductSubCategoryLevel2s",
                table: "ProductSubCategoryLevel2s");

            migrationBuilder.RenameTable(
                name: "SubCategoryLevel2s",
                newName: "SubCategoryLevel2");

            migrationBuilder.RenameTable(
                name: "SubCategoryLevel1s",
                newName: "SubCategoryLevel1");

            migrationBuilder.RenameTable(
                name: "ProductSubCategoryLevel2s",
                newName: "ProductSubCategoryLevel2");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategoryLevel2s_Uid",
                table: "SubCategoryLevel2",
                newName: "IX_SubCategoryLevel2_Uid");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategoryLevel2s_SubCategoryLevel1Id",
                table: "SubCategoryLevel2",
                newName: "IX_SubCategoryLevel2_SubCategoryLevel1Id");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategoryLevel1s_Uid",
                table: "SubCategoryLevel1",
                newName: "IX_SubCategoryLevel1_Uid");

            migrationBuilder.RenameIndex(
                name: "IX_SubCategoryLevel1s_CategoryId",
                table: "SubCategoryLevel1",
                newName: "IX_SubCategoryLevel1_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSubCategoryLevel2s_Uid",
                table: "ProductSubCategoryLevel2",
                newName: "IX_ProductSubCategoryLevel2_Uid");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSubCategoryLevel2s_SubCategoryLevel2Id",
                table: "ProductSubCategoryLevel2",
                newName: "IX_ProductSubCategoryLevel2_SubCategoryLevel2Id");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSubCategoryLevel2s_ProductId",
                table: "ProductSubCategoryLevel2",
                newName: "IX_ProductSubCategoryLevel2_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubCategoryLevel2",
                table: "SubCategoryLevel2",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubCategoryLevel1",
                table: "SubCategoryLevel1",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductSubCategoryLevel2",
                table: "ProductSubCategoryLevel2",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubCategoryLevel2_Products_ProductId",
                table: "ProductSubCategoryLevel2",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSubCategoryLevel2_SubCategoryLevel2_SubCategoryLevel~",
                table: "ProductSubCategoryLevel2",
                column: "SubCategoryLevel2Id",
                principalTable: "SubCategoryLevel2",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategoryLevel1_Categories_CategoryId",
                table: "SubCategoryLevel1",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SubCategoryLevel2_SubCategoryLevel1_SubCategoryLevel1Id",
                table: "SubCategoryLevel2",
                column: "SubCategoryLevel1Id",
                principalTable: "SubCategoryLevel1",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
