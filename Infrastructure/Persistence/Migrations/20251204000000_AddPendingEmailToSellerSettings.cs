using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingEmailToSellerSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if the column exists before trying to add it
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Add column only if it doesn't exist
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'SellerSettings' 
                        AND column_name = 'PendingEmail'
                    ) THEN
                        ALTER TABLE ""SellerSettings"" ADD ""PendingEmail"" text;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "SellerSettings");
        }
    }
}

