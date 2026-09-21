namespace DottIn.Admin.Models;

public record LoginRequest(string Cpf, string Password, string? CompanyJoinToken = null);
public record PinLoginRequest(string Cpf, string Pin, string CompanyCode);
public record ClockInRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude, bool SkipGeolocationValidation = false, string Source = "Web");
public record ClockOutRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude, bool SkipGeolocationValidation = false, string Source = "Web");
public record BreakRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude, bool SkipGeolocationValidation = false, string Source = "Web");
public record ClockInResponse(Guid TimeKeepingId);

public record LoginResponse(
    string AccessToken, string RefreshToken, DateTime ExpiresAt,
    EmployeeInfo Employee, Guid BranchId, bool IsOwner, bool IsHeadquarters, string CompanyCode);

public record RefreshTokenRequest(string RefreshToken);
public record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public record EmployeeInfo(Guid Id, string Name, string Cpf, string? ImageUrl)
{
    public string FirstName => Name.Split(' ').FirstOrDefault() ?? Name;
    public string Initials => string.Join("", Name.Split(' ').Take(2).Select(n => n.FirstOrDefault()));
}

public record BranchSummary(
    Guid Id, string Name, string? Email, string? PhoneNumber,
    bool IsActive, bool IsHeadquarters, string OwnerName);

public record BranchClockResponse(DateTime UtcNow, DateTime LocalNow, string TimeZoneId);

public record EmployeeSummary(
    Guid EmployeeId, string Name, DocumentInfo Document, string? ImageUrl,
    string BranchName, TimeOnly StartWorkTime, TimeOnly EndWorkTime,
    bool IsActive, bool HasFingerprint);

public record DocumentInfo(string Value, string Type);

public record TimeKeepingRecord(
    Guid Id, Guid EmployeeId, string EmployeeName, DateOnly WorkDate,
    DateTime? ClockIn, DateTime? ClockOut,
    TimeSpan TotalWorked, TimeSpan TotalBreak,
    string Status, bool IsNocturnal, string Source,
    bool IsHoliday = false, string? HolidayName = null,
    TimeSpan NocturnalWorked = default, TimeSpan ExpectedWorked = default,
    TimeSpan Late = default, TimeSpan EarlyDeparture = default, TimeSpan Overtime = default);

public record TimeKeepingDetails(
    string EmployeeName, string BranchName, string Status,
    DateOnly WorkDate, DateTime CreatedAt,
    GeolocationInfo? GeolocationDto,
    IEnumerable<TimeEntryInfo> EntriesDto, bool IsNocturnal, string Source,
    bool IsHoliday = false, string? HolidayName = null);

public record GeolocationInfo(double Latitude, double Longitude);
public record TimeEntryInfo(DateTime Timestamp, string Type);

public record CreateTimeKeepingAdjustmentRequest(
    string EntryType, DateTime? OriginalTimestamp, DateTime ProposedTimestamp, string Reason);
public record ReviewTimeKeepingAdjustmentRequest(bool Approve, string? ReviewNote);
public record TimeKeepingAdjustmentItem(
    Guid Id, Guid TimeKeepingId, Guid EmployeeId, string EmployeeName,
    Guid RequestedByEmployeeId, string RequestedByName,
    Guid? ReviewedByEmployeeId, string? ReviewedByName,
    string EntryType, DateTime? OriginalTimestamp, DateTime ProposedTimestamp,
    string Reason, string? ReviewNote, string Status, DateTime CreatedAt, DateTime? ReviewedAt);

public record ApiProblem(int Status, string? Title, object? Errors);

public class ApiException(string message) : Exception(message);

// Holiday Calendar models
public record HolidayCalendarSummary(
    Guid Id, string BranchName, string Name, string? Description,
    string CountryCode, string? RegionCode, int Year, bool IsActive, int HolidayCount);

public record HolidayCalendarDetails(
    Guid Id, string BranchName, string Name, string? Description,
    string CountryCode, string? RegionCode, int Year, bool IsActive,
    DateTime CreatedAt, DateTime? UpdatedAt, IEnumerable<HolidayItem> Holidays);

public record HolidayItem(DateOnly Date, string Name, string Type, bool IsOptional);

public record CreateHolidayCalendarRequest(string Name, string CountryCode, int Year, string? RegionCode, string? Description);
public record AddHolidaysRequest(IEnumerable<HolidayItemRequest> Holidays);
public record HolidayItemRequest(DateOnly Date, string Name, string Type, bool IsOptional);
public record UpdateHolidayRequest(string? NewName, string? NewType, bool? IsOptional);

// Domínio Export models
public record DominioMappingDto(Guid EmployeeId, string EmployeeName, string EmployeeDocument, string DominioCode);
public record SaveDominioMappingRequest(Guid EmployeeId, string DominioCode);

// Billing models
public record SubscriptionPlan(
    Guid Id,
    string Name,
    string? StripePriceId,
    int MaxEmployees,
    int MaxBranches,
    decimal MonthlyPriceBRL,
    bool HasUnlimitedEmployees,
    bool HasUnlimitedBranches);

public record BillingInfo(
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

public record CreateCheckoutSessionRequest(Guid PlanId);
public record CheckoutSessionResponse(string CheckoutUrl);
public record PortalSessionResponse(string PortalUrl);
public record CompanyJoinLinkResponse(string Token, DateTime ExpiresAt, string CompanyName);
public record CompanyJoinLinkResolutionResponse(string CompanyName, bool CanJoin);
public record RegisterFromCompanyJoinLinkRequest(string Token, string Name, string Cpf, string Password);
public record RegisterFromCompanyJoinLinkResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, Guid EmployeeId, Guid BranchId);
