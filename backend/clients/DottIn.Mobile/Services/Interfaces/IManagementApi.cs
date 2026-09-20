using Refit;

namespace DottIn.Mobile.Services.Interfaces;

public interface IBillingApi
{
    [Get("/api/billing/plans")]
    Task<List<SubscriptionPlan>> GetPlansAsync();
    [Get("/api/billing/subscription")]
    Task<BillingInfo> GetSubscriptionAsync();
    [Post("/api/billing/checkout-session")]
    Task<CheckoutSessionResponse> CheckoutAsync([Body] CheckoutSessionRequest request);
    [Post("/api/billing/portal-session")]
    Task<PortalSessionResponse> PortalAsync();
}

public record OwnerRegistrationRequest(string Name, DocumentInfo Document, string Password);
public record OwnerRegistrationResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, EmployeeInfo Employee);
public record EmployeeDirectoryItem(Guid EmployeeId, string Name, DocumentInfo Document, string? ImageUrl,
    string BranchName, TimeOnly StartWorkTime, TimeOnly EndWorkTime, bool IsActive, bool HasFingerprint);
public record BranchContext(string Name, string CompanyCode);
public record CreateBranchResponse(Guid BranchId, string CompanyCode);
public record BranchAddress(string Street, int Number, string City, string State, string ZipCode, string? Complement);
public record CreateBranchRequest(string Name, DocumentInfo Document, GeolocationInfo Geolocation,
    BranchAddress Address, string TimeZoneId, string StartWorkTime, string EndWorkTime,
    string Email, string PhoneNumber, Guid OwnerId, bool IsHeadQuarters, int AllowedRadiusMeters, int ToleranceMinutes);
public record SubscriptionPlan(Guid Id, string Name, string? StripePriceId, int MaxEmployees, int MaxBranches,
    decimal MonthlyPriceBRL, bool HasUnlimitedEmployees, bool HasUnlimitedBranches);
public record BillingInfo(Guid SubscriptionId, string PlanName, string Status, int MaxEmployees, int MaxBranches,
    int CurrentEmployeeCount, int CurrentBranchCount, DateTime CurrentPeriodEnd, bool CanAddEmployee, bool CanAddBranch);
public record CheckoutSessionRequest(Guid PlanId);
public record CheckoutSessionResponse(string CheckoutUrl);
public record PortalSessionResponse(string PortalUrl);
