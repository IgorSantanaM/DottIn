using System.Security.Claims;
using DottIn.Application.Features.Subscriptions.Services;
using DottIn.Domain.Branches;
using DottIn.Domain.Subscriptions;
using DottIn.Infra.Services.Stripe;
using DottIn.Presentation.WebApi.DTOs.Billing;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DottIn.Presentation.WebApi.Endpoints
{
    public class BillingEndpoints : IEndpoint
    {
        private const string Tag = "Billing";

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

            var response = plans.Select(p => new SubscriptionPlanResponse(
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
            ClaimsPrincipal user,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] ITenantSubscriptionService subscriptionService,
            CancellationToken cancellationToken)
        {
            var branchIdClaim = user.FindFirstValue("BranchId");
            if (string.IsNullOrEmpty(branchIdClaim) || !Guid.TryParse(branchIdClaim, out var branchId))
                return Results.Unauthorized();

            var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
            if (branch?.OwnerId == null)
                return Results.NotFound(new { Message = "Filial não encontrada ou sem proprietário." });

            var subscription = await subscriptionService.GetByOwnerIdAsync(branch.OwnerId.Value, cancellationToken);
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
            ClaimsPrincipal user,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] ITenantSubscriptionRepository subscriptionRepository,
            [FromServices] IStripeService stripeService,
            CancellationToken cancellationToken)
        {
            var branchIdClaim = user.FindFirstValue("BranchId");
            if (string.IsNullOrEmpty(branchIdClaim) || !Guid.TryParse(branchIdClaim, out var branchId))
                return Results.Unauthorized();

            var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
            if (branch?.OwnerId == null)
                return Results.NotFound(new { Message = "Filial não encontrada ou sem proprietário." });

            var subscription = await subscriptionRepository.GetByOwnerIdAsync(branch.OwnerId.Value, cancellationToken);
            if (subscription == null)
                return Results.NotFound(new { Message = "Assinatura não encontrada. Por favor, entre em contato com o suporte." });

            if (string.IsNullOrEmpty(request.PriceId))
                return Results.BadRequest(new { Message = "PriceId é obrigatório." });

            var checkoutUrl = await stripeService.CreateCheckoutSessionAsync(
                subscription.StripeCustomerId,
                request.PriceId,
                subscription.HeadquartersId,
                cancellationToken);

            return Results.Ok(new CheckoutSessionResponse(checkoutUrl));
        }

        private static async Task<IResult> HandleCreatePortalSessionAsync(
            ClaimsPrincipal user,
            [FromServices] IBranchRepository branchRepository,
            [FromServices] ITenantSubscriptionRepository subscriptionRepository,
            [FromServices] IStripeService stripeService,
            CancellationToken cancellationToken)
        {
            var branchIdClaim = user.FindFirstValue("BranchId");
            if (string.IsNullOrEmpty(branchIdClaim) || !Guid.TryParse(branchIdClaim, out var branchId))
                return Results.Unauthorized();

            var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
            if (branch?.OwnerId == null)
                return Results.NotFound(new { Message = "Filial não encontrada ou sem proprietário." });

            var subscription = await subscriptionRepository.GetByOwnerIdAsync(branch.OwnerId.Value, cancellationToken);
            if (subscription == null)
                return Results.NotFound(new { Message = "Assinatura não encontrada." });

            var portalUrl = await stripeService.CreateCustomerPortalSessionAsync(
                subscription.StripeCustomerId,
                cancellationToken);

            return Results.Ok(new PortalSessionResponse(portalUrl));
        }
    }
}
