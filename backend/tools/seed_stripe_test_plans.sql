-- Development/test only. Supply price IDs from the SAME Stripe test account as
-- Stripe:SecretKey. The IDs must be active BRL monthly prices of R$60, R$160
-- and R$430, respectively. Do not apply this file to production.
--
-- psql -v basic_price=price_... -v professional_price=price_... \
--      -v unlimited_price=price_... -f seed_stripe_test_plans.sql

\if :{?basic_price}
\else
  \echo 'Missing -v basic_price=price_...'
  \quit 2
\endif
\if :{?professional_price}
\else
  \echo 'Missing -v professional_price=price_...'
  \quit 2
\endif
\if :{?unlimited_price}
\else
  \echo 'Missing -v unlimited_price=price_...'
  \quit 2
\endif

BEGIN;

INSERT INTO "SubscriptionPlans" (
    "Id", "Name", "StripePriceId", "MaxEmployees", "MaxBranches",
    "MonthlyPriceBRL", "FeaturesJson", "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    md5('stripe-test-plan:Free')::uuid, 'Free', NULL, 5, 1,
    0, NULL, true, now(), NULL
)
ON CONFLICT ("Name") DO UPDATE SET
    "IsActive" = true,
    "UpdatedAt" = now();

INSERT INTO "SubscriptionPlans" (
    "Id", "Name", "StripePriceId", "MaxEmployees", "MaxBranches",
    "MonthlyPriceBRL", "FeaturesJson", "IsActive", "CreatedAt", "UpdatedAt"
) VALUES
    (md5('stripe-test-plan:Basic')::uuid, 'Basic', :'basic_price', 5, 1, 60, NULL, true, now(), NULL),
    (md5('stripe-test-plan:Professional')::uuid, 'Professional', :'professional_price', 50, 5, 160, NULL, true, now(), NULL),
    (md5('stripe-test-plan:Unlimited')::uuid, 'Unlimited', :'unlimited_price', -1, -1, 430, NULL, true, now(), NULL)
ON CONFLICT ("Name") DO UPDATE SET
    "StripePriceId" = EXCLUDED."StripePriceId",
    "MaxEmployees" = EXCLUDED."MaxEmployees",
    "MaxBranches" = EXCLUDED."MaxBranches",
    "MonthlyPriceBRL" = EXCLUDED."MonthlyPriceBRL",
    "IsActive" = true,
    "UpdatedAt" = now();

COMMIT;
