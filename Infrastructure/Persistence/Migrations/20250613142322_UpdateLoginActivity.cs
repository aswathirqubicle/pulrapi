using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    public partial class UpdateLoginActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeviceName",
                table: "UserLoginActivities",
                newName: "OsVersion");

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "UserLoginActivities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "UserLoginActivities",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "UserLoginActivities");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "UserLoginActivities");

            migrationBuilder.RenameColumn(
                name: "OsVersion",
                table: "UserLoginActivities",
                newName: "DeviceName");
        }
    }
}
