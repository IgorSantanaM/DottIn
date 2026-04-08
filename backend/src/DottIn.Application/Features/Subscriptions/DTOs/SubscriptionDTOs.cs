namespace DottIn.Application.Features.Subscriptions.DTOs
{
    public record SubscriptionPlanDto(
        Guid Id,
        string Name,
        string? StripePriceId,
        int MaxEmployees,
        int MaxBranches,
        decimal MonthlyPriceBRL,
        bool HasUnlimitedEmployees,
        bool HasUnlimitedBranches);

    public record TenantSubscriptionDto(
        Guid Id,
        Guid HeadquartersId,
        Guid OwnerId,
        string PlanName,
        string Status,
        int MaxEmployees,
        int MaxBranches,
        int CurrentEmployeeCount,
        int CurrentBranchCount,
        DateTime CurrentPeriodEnd,
        bool CanAddEmployee,
        bool CanAddBranch);

    public record BillingInfoDto(
        TenantSubscriptionDto Subscription,
        IEnumerable<SubscriptionPlanDto> AvailablePlans);
}
