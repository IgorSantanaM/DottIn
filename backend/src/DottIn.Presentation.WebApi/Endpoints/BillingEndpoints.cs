using System.Security.Claims;
using DottIn.Application.Features.Subscriptions.Services;
using DottIn.Domain.Branches;
using DottIn.Domain.Subscriptions;
using DottIn.Infra.Services.Stripe;
using DottIn.Presentation.WebApi.DTOs.Billing;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DottIn.Presentation.WebApi.Security;

namespace DottIn.Presentation.WebApi.Endpoints
{
    public class BillingEndpoints : IEndpoint
    {
        private const string Tag = "Billing";
        private static readonly string[] PublicPlanNames = ["Basic", "Starter", "Pro"];

        public static void DefineEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/billing")
                .WithTags(Tag)
                .RequireAuthorization();

            group.MapGet("/config", HandleGetConfigAsync)
                .WithName(nameof(HandleGetConfigAsync))
                .WithSummary("Get Stripe configuration")
                .WithDescription("Returns the Stripe publishable key for frontend integration.")
                .Produces<StripeConfigResponse>(StatusCodes.Status200OK)
                .AllowAnonymous();

            group.MapGet("/plans", HandleGetPlansAsync)
                .WithName(nameof(HandleGetPlansAsync))
                .WithSummary("Get available subscription plans")
                .WithDescription("Returns all active subscription plans with pricing information.")
                .Produces<IEnumerable<SubscriptionPlanResponse>>(StatusCodes.Status200OK)
                .AllowAnonymous();

            group.MapGet("/subscription", HandleGetSubscriptionAsync)
                .WithName(nameof(HandleGetSubscriptionAsync))
                .WithSummary("Get current subscription")
                .WithDescription("Returns the current subscription details for the authenticated user's HQ.")
                .Produces<BillingInfoResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);

            group.MapPost("/checkout-session", HandleCreateCheckoutSessionAsync)
                .WithName(nameof(HandleCreateCheckoutSessionAsync))
                .WithSummary("Create Stripe checkout session")
                .WithDescription("Creates a Stripe checkout session for upgrading the subscription plan.")
                .Produces<CheckoutSessionResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound);

            group.MapPost("/portal-session", HandleCreatePortalSessionAsync)
                .WithName(nameof(HandleCreatePortalSessionAsync))
                .WithSummary("Create Stripe portal session")
                .WithDescription("Creates a Stripe customer portal session for managing the subscription.")
                .Produces<PortalSessionResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
        }

        private static IResult HandleGetConfigAsync(
            [FromServices] IOptions<StripeSettings> stripeSettings)
        {
            return Results.Ok(new StripeConfigResponse(stripeSettings.Value.PublishableKey));
        }

        private static async Task<IResult> HandleGetPlansAsync(
            [FromServices] ISubscriptionPlanRepository planRepository,
            CancellationToken cancellationToken)
        {
            var plans = await planRepository.GetAllActiveAsync(cancellationToken);

            var response = plans
                .Where(p => PublicPlanNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .Select(p => new SubscriptionPlanResponse(
                    p.Id,
                    p.Name,
                    p.StripePriceId,
                    p.MaxEmployees,
                    p.MaxBranches,
                    p.MonthlyPriceBRL,
                    p.HasUnlimitedEmployees,
                    p.HasUnlimitedBranches));

            return Results.Ok(response);
        }

        private static async Task<IResult> HandleGetSubscriptionAsync(
            [FromServices] CurrentUserContext currentUser,
            [FromServices] ITenantSubscriptionService subscriptionService,
            CancellationToken cancellationToken)
        {
            var subscription = await subscriptionService.GetByOwnerIdAsync(currentUser.TenantId, cancellationToken);
            if (subscription == null)
                return Results.NotFound(new { Message = "Assinatura não encontrada." });

            var response = new BillingInfoResponse(
                subscription.Id,
                subscription.PlanName,
                subscription.Status,
                subscription.MaxEmployees,
                subscription.MaxBranches,
                subscription.CurrentEmployeeCount,
                subscription.CurrentBranchCount,
                subscription.CurrentPeriodEnd,
                subscription.CanAddEmployee,
                subscription.CanAddBranch);

            return Results.Ok(response);
        }

        private static async Task<IResult> HandleCreateCheckoutSessionAsync(
            [FromBody] CreateCheckoutSessionRequest request,
            [FromServices] CurrentUserContext currentUser,
            [FromServices] ITenantSubscriptionRepository subscriptionRepository,
            [FromServices] ISubscriptionPlanRepository planRepository,
            [FromServices] IStripeService stripeService,
            CancellationToken cancellationToken)
        {
            if (!currentUser.IsAdministrator)
                return Results.Forbid();

            var subscription = await subscriptionRepository.GetByOwnerIdAsync(currentUser.TenantId, cancellationToken);
            if (subscription is null)
                return Results.NotFound(new { Message = "Assinatura não encontrada. Por favor, entre em contato com o suporte." });

            if (subscription.IsPaid && !string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
                return Results.Conflict(new { Message = "Use o portal de cobrança para alterar uma assinatura existente." });

            var plan = await planRepository.GetByIdAsync(request.PlanId, cancellationToken);
            if (plan is null || !plan.IsActive ||
                !PublicPlanNames.Contains(plan.Name, StringComparer.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(plan.StripePriceId))
                return Results.BadRequest(new { Message = "Plano indisponível para contratação." });

            var checkoutUrl = await stripeService.CreateCheckoutSessionAsync(
                subscription.StripeCustomerId,
                plan.StripePriceId,
                subscription.HeadquartersId,
                cancellationToken);

            return Results.Ok(new CheckoutSessionResponse(checkoutUrl));
        }

        private static async Task<IResult> HandleCreatePortalSessionAsync(
            [FromServices] CurrentUserContext currentUser,
            [FromServices] ITenantSubscriptionRepository subscriptionRepository,
            [FromServices] IStripeService stripeService,
            CancellationToken cancellationToken)
        {
            if (!currentUser.IsAdministrator)
                return Results.Forbid();

            var subscription = await subscriptionRepository.GetByOwnerIdAsync(currentUser.TenantId, cancellationToken);
            if (subscription is null)
                return Results.NotFound(new { Message = "Assinatura não encontrada." });

            var portalUrl = await stripeService.CreateCustomerPortalSessionAsync(
                subscription.StripeCustomerId,
                cancellationToken);

            return Results.Ok(new PortalSessionResponse(portalUrl));
        }
    }
}
