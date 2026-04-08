using DottIn.Domain.Core.Data;
using DottIn.Domain.Subscriptions;
using DottIn.Infra.Services.Stripe;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace DottIn.Presentation.WebApi.Endpoints
{
    public class StripeWebhookEndpoints : IEndpoint
    {
        private const string Tag = "Webhooks";

        public static void DefineEndpoints(WebApplication app)
        {
            app.MapPost("/api/webhooks/stripe", HandleStripeWebhookAsync)
                .WithTags(Tag)
                .WithName(nameof(HandleStripeWebhookAsync))
                .WithSummary("Stripe webhook endpoint")
                .WithDescription("Handles incoming Stripe webhook events for subscription management.")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .AllowAnonymous()
                .DisableAntiforgery();
        }

        private static async Task<IResult> HandleStripeWebhookAsync(
            HttpRequest request,
            [FromServices] IStripeService stripeService,
            [FromServices] ITenantSubscriptionRepository subscriptionRepository,
            [FromServices] ISubscriptionPlanRepository planRepository,
            [FromServices] IUnitOfWork unitOfWork,
            [FromServices] ILogger<StripeWebhookEndpoints> logger,
            CancellationToken cancellationToken)
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync(cancellationToken);
            var signature = request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("Stripe webhook received without signature");
                return Results.BadRequest(new { Message = "Missing Stripe signature" });
            }

            var stripeEvent = stripeService.ParseWebhookEvent(json, signature);
            if (stripeEvent is null)
            {
                logger.LogWarning("Failed to parse Stripe webhook event");
                return Results.BadRequest(new { Message = "Invalid webhook signature" });
            }

            logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

            try
            {
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        await HandleCheckoutSessionCompleted(stripeEvent, subscriptionRepository, planRepository, unitOfWork, logger, cancellationToken);
                        break;

                    case "customer.subscription.updated":
                        await HandleSubscriptionUpdated(stripeEvent, subscriptionRepository, planRepository, unitOfWork, logger, cancellationToken);
                        break;

                    case "customer.subscription.deleted":
                        await HandleSubscriptionDeleted(stripeEvent, subscriptionRepository, planRepository, unitOfWork, logger, cancellationToken);
                        break;

                    case "invoice.payment_failed":
                        await HandlePaymentFailed(stripeEvent, subscriptionRepository, unitOfWork, logger, cancellationToken);
                        break;

                    case "invoice.payment_succeeded":
                        await HandlePaymentSucceeded(stripeEvent, subscriptionRepository, unitOfWork, logger, cancellationToken);
                        break;

                    default:
                        logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                        break;
                }

                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Stripe webhook event: {EventType}", stripeEvent.Type);
                return Results.BadRequest(new { Message = "Error processing webhook" });
            }
        }

        private static async Task HandleCheckoutSessionCompleted(
            Event stripeEvent,
            ITenantSubscriptionRepository subscriptionRepository,
            ISubscriptionPlanRepository planRepository,
            IUnitOfWork unitOfWork,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session is null)
            {
                logger.LogWarning("checkout.session.completed: Could not parse session object");
                return;
            }

            var customerId = session.CustomerId;
            var subscriptionId = session.SubscriptionId;

            if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(subscriptionId))
            {
                logger.LogWarning("checkout.session.completed: Missing customerId or subscriptionId");
                return;
            }

            var tenantSubscription = await subscriptionRepository.GetByStripeCustomerIdAsync(customerId, cancellationToken);
            if (tenantSubscription is null)
            {
                logger.LogWarning("checkout.session.completed: No subscription found for customer {CustomerId}", customerId);
                return;
            }

            var subscriptionService = new SubscriptionService();
            var stripeSubscription = await subscriptionService.GetAsync(subscriptionId, cancellationToken: cancellationToken);

            var firstItem = stripeSubscription.Items.Data.FirstOrDefault();
            var priceId = firstItem?.Price.Id;
            if (string.IsNullOrEmpty(priceId))
            {
                logger.LogWarning("checkout.session.completed: No price ID found in subscription");
                return;
            }

            var plan = await planRepository.GetByStripePriceIdAsync(priceId, cancellationToken);
            if (plan is null)
            {
                logger.LogWarning("checkout.session.completed: No plan found for price {PriceId}", priceId);
                return;
            }

            var periodStart = firstItem!.CurrentPeriodStart;
            var periodEnd = firstItem.CurrentPeriodEnd;

            tenantSubscription.Activate(subscriptionId, plan.Id, periodStart, periodEnd);
            await subscriptionRepository.UpdateAsync(tenantSubscription);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("checkout.session.completed: Activated subscription {SubscriptionId} for customer {CustomerId} with plan {PlanName}",
                subscriptionId, customerId, plan.Name);
        }

        private static async Task HandleSubscriptionUpdated(
            Event stripeEvent,
            ITenantSubscriptionRepository subscriptionRepository,
            ISubscriptionPlanRepository planRepository,
            IUnitOfWork unitOfWork,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (stripeSubscription is null)
            {
                logger.LogWarning("customer.subscription.updated: Could not parse subscription object");
                return;
            }

            var tenantSubscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscription.Id, cancellationToken);
            if (tenantSubscription is null)
            {
                logger.LogWarning("customer.subscription.updated: No tenant subscription found for {SubscriptionId}", stripeSubscription.Id);
                return;
            }

            var firstItem = stripeSubscription.Items.Data.FirstOrDefault();
            if (firstItem is null)
            {
                logger.LogWarning("customer.subscription.updated: No subscription items found for {SubscriptionId}", stripeSubscription.Id);
                return;
            }

            var priceId = firstItem.Price.Id;
            if (!string.IsNullOrEmpty(priceId))
            {
                var plan = await planRepository.GetByStripePriceIdAsync(priceId, cancellationToken);
                if (plan is not null && plan.Id != tenantSubscription.SubscriptionPlanId)
                {
                    tenantSubscription.UpdatePlan(plan.Id, firstItem.CurrentPeriodStart, firstItem.CurrentPeriodEnd);
                    logger.LogInformation("customer.subscription.updated: Updated plan to {PlanName} for subscription {SubscriptionId}",
                        plan.Name, stripeSubscription.Id);
                }
            }

            if (stripeSubscription.Status == "active" && tenantSubscription.Status != SubscriptionStatus.Active)
            {
                tenantSubscription.MarkActive();
            }
            else if (stripeSubscription.Status == "past_due")
            {
                tenantSubscription.MarkPastDue();
            }

            tenantSubscription.UpdatePeriod(firstItem.CurrentPeriodStart, firstItem.CurrentPeriodEnd);

            await subscriptionRepository.UpdateAsync(tenantSubscription);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static async Task HandleSubscriptionDeleted(
            Event stripeEvent,
            ITenantSubscriptionRepository subscriptionRepository,
            ISubscriptionPlanRepository planRepository,
            IUnitOfWork unitOfWork,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (stripeSubscription is null)
            {
                logger.LogWarning("customer.subscription.deleted: Could not parse subscription object");
                return;
            }

            var tenantSubscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(stripeSubscription.Id, cancellationToken);
            if (tenantSubscription is null)
            {
                logger.LogWarning("customer.subscription.deleted: No tenant subscription found for {SubscriptionId}", stripeSubscription.Id);
                return;
            }

            var freePlan = await planRepository.GetFreePlanAsync(cancellationToken);
            if (freePlan is null)
            {
                logger.LogError("customer.subscription.deleted: Free plan not found in database");
                tenantSubscription.Cancel();
            }
            else
            {
                tenantSubscription.RevertToFree(freePlan.Id);
                logger.LogInformation("customer.subscription.deleted: Reverted subscription {SubscriptionId} to Free plan", stripeSubscription.Id);
            }

            await subscriptionRepository.UpdateAsync(tenantSubscription);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static async Task HandlePaymentFailed(
            Event stripeEvent,
            ITenantSubscriptionRepository subscriptionRepository,
            IUnitOfWork unitOfWork,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice is null)
            {
                logger.LogWarning("invoice.payment_failed: Could not parse invoice object");
                return;
            }

            var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
            if (string.IsNullOrEmpty(subscriptionId))
            {
                logger.LogDebug("invoice.payment_failed: Invoice has no subscription ID, skipping");
                return;
            }

            var tenantSubscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(subscriptionId, cancellationToken);
            if (tenantSubscription is null)
            {
                logger.LogWarning("invoice.payment_failed: No tenant subscription found for {SubscriptionId}", subscriptionId);
                return;
            }

            tenantSubscription.MarkPastDue();
            await subscriptionRepository.UpdateAsync(tenantSubscription);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogWarning("invoice.payment_failed: Marked subscription {SubscriptionId} as past due", subscriptionId);
        }

        private static async Task HandlePaymentSucceeded(
            Event stripeEvent,
            ITenantSubscriptionRepository subscriptionRepository,
            IUnitOfWork unitOfWork,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice is null)
            {
                logger.LogWarning("invoice.payment_succeeded: Could not parse invoice object");
                return;
            }

            var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
            if (string.IsNullOrEmpty(subscriptionId))
            {
                logger.LogDebug("invoice.payment_succeeded: Invoice has no subscription ID, skipping");
                return;
            }

            var tenantSubscription = await subscriptionRepository.GetByStripeSubscriptionIdAsync(subscriptionId, cancellationToken);
            if (tenantSubscription is null)
            {
                logger.LogDebug("invoice.payment_succeeded: No tenant subscription found for {SubscriptionId}", subscriptionId);
                return;
            }

            if (tenantSubscription.Status == SubscriptionStatus.PastDue)
            {
                tenantSubscription.MarkActive();
                await subscriptionRepository.UpdateAsync(tenantSubscription);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation("invoice.payment_succeeded: Reactivated subscription {SubscriptionId}", subscriptionId);
            }
        }
    }
}
