using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DottIn.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedFreeSubscriptionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First-branch onboarding needs Free even when no test seed has run.
            // Preserve any existing plan ID, price and limits used by current tenants.
            migrationBuilder.Sql("""
                INSERT INTO "SubscriptionPlans" (
                    "Id", "Name", "StripePriceId", "MaxEmployees", "MaxBranches",
                    "MonthlyPriceBRL", "FeaturesJson", "IsActive", "CreatedAt", "UpdatedAt"
                ) VALUES (
                    md5('dottin-plan:Free')::uuid, 'Free', NULL, 5, 1,
                    0, NULL, TRUE, now(), NULL
                )
                ON CONFLICT ("Name") DO UPDATE SET
                    "IsActive" = TRUE,
                    "UpdatedAt" = CASE WHEN "SubscriptionPlans"."IsActive"
                        THEN "SubscriptionPlans"."UpdatedAt" ELSE now() END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep the catalog row: tenant subscriptions may reference it.
        }
    }
}
