namespace DottIn.Presentation.WebApi.DTOs.Billing
{
    public record StripeConfigResponse(string PublishableKey);

    public record CreateCheckoutSessionRequest(string PriceId);

    public record CheckoutSessionResponse(string CheckoutUrl);

    public record PortalSessionResponse(string PortalUrl);

    public record SubscriptionPlanResponse(
        Guid Id,
        string Name,
        string? StripePriceId,
        int MaxEmployees,
        int MaxBranches,
        decimal MonthlyPriceBRL,
        bool HasUnlimitedEmployees,
        bool HasUnlimitedBranches);

    public record BillingInfoResponse(
        Guid SubscriptionId,
        string PlanName,
        string Status,
        int MaxEmployees,
        int MaxBranches,
        int CurrentEmployeeCount,
        int CurrentBranchCount,
        DateTime CurrentPeriodEnd,
        bool CanAddEmployee,
        bool CanAddBranch);
}
