using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds EmailVerificationCode and EmailVerificationCodeExpiry columns to AspNetUsers table.
    /// This migration ensures the columns are properly created for email verification functionality.
    /// </summary>
    public partial class ForceAddEmailVerificationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if EmailVerificationCode column exists before adding
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'AspNetUsers' 
                        AND column_name = 'EmailVerificationCode'
                    ) THEN
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""EmailVerificationCode"" text;
                    END IF;
                END $$;
            ");

            // Check if EmailVerificationCodeExpiry column exists before adding
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'AspNetUsers' 
                        AND column_name = 'EmailVerificationCodeExpiry'
                    ) THEN
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""EmailVerificationCodeExpiry"" timestamp with time zone;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationCodeExpiry",
                table: "AspNetUsers");
        }
    }
}
