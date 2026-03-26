using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCountdownExpiryDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill CountdownExpiryDate for existing orders where it is null.
            // Parses DeliveryTimeSnapshot to extract the number of days (takes the max value for ranges)
            // and adds that to CreatedAt. Falls back to 7 days if snapshot is null or unparseable.
            migrationBuilder.Sql(@"
                UPDATE ""OrderProductAffiliates""
                SET ""CountdownExpiryDate"" = ""CreatedAt"" + (
                    CASE
                        -- Range like ""7-10 days"" or ""1-2 weeks"" — take upper number
                        WHEN ""DeliveryTimeSnapshot"" ~* '(\d+)\s*-\s*(\d+)\s*week'
                            THEN (REGEXP_REPLACE(""DeliveryTimeSnapshot"", '.*(\d+)\s*-\s*(\d+)\s*week.*', '\2'))::int * 7 * INTERVAL '1 day'
                        WHEN ""DeliveryTimeSnapshot"" ~* '(\d+)\s*-\s*(\d+)\s*day'
                            THEN (REGEXP_REPLACE(""DeliveryTimeSnapshot"", '.*(\d+)\s*-\s*(\d+)\s*day.*', '\2'))::int * INTERVAL '1 day'
                        -- Single value like ""7 days"" or ""2 weeks""
                        WHEN ""DeliveryTimeSnapshot"" ~* '(\d+)\s*week'
                            THEN (REGEXP_REPLACE(""DeliveryTimeSnapshot"", '.*?(\d+)\s*week.*', '\1'))::int * 7 * INTERVAL '1 day'
                        WHEN ""DeliveryTimeSnapshot"" ~* '(\d+)\s*(day|minute|hour)'
                            THEN (REGEXP_REPLACE(""DeliveryTimeSnapshot"", '.*?(\d+)\s*(day|minute|hour).*', '\1'))::int * INTERVAL '1 day'
                        -- Any other number found
                        WHEN ""DeliveryTimeSnapshot"" ~* '\d+'
                            THEN (REGEXP_REPLACE(""DeliveryTimeSnapshot"", '.*?(\d+).*', '\1'))::int * INTERVAL '1 day'
                        ELSE 7 * INTERVAL '1 day'
                    END
                )
                WHERE ""CountdownExpiryDate"" IS NULL
                  AND ""IsActive"" = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot safely undo a data backfill — set all backfilled values back to null
            // Only rows that were originally null would be affected; we can't distinguish them after the fact
            // so this is intentionally left as no-op to protect data
        }
    }
}
