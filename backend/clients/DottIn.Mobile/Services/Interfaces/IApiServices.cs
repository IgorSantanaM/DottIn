using Refit;

namespace DottIn.Mobile.Services.Interfaces;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request);

    [Post("/api/auth/login/pin")]
    Task<LoginResponse> LoginWithPinAsync([Body] PinLoginRequest request);

    [Post("/api/auth/login/fingerprint")]
    Task<LoginResponse> LoginWithFingerprintAsync([Body] FingerprintLoginRequest request);

    [Post("/api/auth/register-fingerprint")]
    Task RegisterFingerprintAsync([Body] RegisterFingerprintRequest request);

    [Put("/api/auth/change-password")]
    Task ChangePasswordAsync([Body] ChangePasswordRequest request);

    [Put("/api/auth/change-pin")]
    Task ChangePinAsync([Body] ChangePinRequest request);

    [Post("/api/auth/refresh")]
    Task<TokenResponse> RefreshTokenAsync([Body] RefreshTokenRequest request);

    [Post("/api/auth/logout")]
    Task LogoutAsync();
}

public interface ITimeKeepingApi
{
    [Post("/api/timekeeping/clock-in")]
    Task<ClockInResponse> ClockInAsync([Body] ClockInRequest request);

    [Post("/api/timekeeping/clock-out")]
    Task ClockOutAsync([Body] ClockOutRequest request);

    [Post("/api/timekeeping/break")]
    Task BreakAsync([Body] BreakRequest request);

    [Get("/api/timekeeping/employee/{employeeId}/history")]
    Task<IEnumerable<TimeKeepingRecord>> GetHistoryAsync(
        Guid employeeId,
        [Query] DateOnly startDate,
        [Query] DateOnly? endDate = null);

    [Get("/api/timekeeping/{timeKeepingId}")]
    Task<TimeKeepingDetails> GetByIdAsync(Guid timeKeepingId);

    [Get("/api/timekeeping/branch/{branchId}/current")]
    Task<IEnumerable<TimeKeepingSummary>> GetCurrentAsync(Guid branchId);

    [Get("/api/timekeeping/branch/{branchId}/history")]
    Task<IEnumerable<BranchTimeKeepingRecord>> GetBranchHistoryAsync(
        Guid branchId,
        [Query] DateOnly startDate,
        [Query] DateOnly? endDate = null);
}

public interface IEmployeeApi
{
    [Get("/api/branches/{branchId}/employees/{employeeId}")]
    Task<EmployeeDetails> GetByIdAsync(Guid branchId, Guid employeeId);

    [Get("/api/branches/{branchId}/employees/active")]
    Task<IEnumerable<EmployeeSummaryItem>> GetActiveByBranchAsync(Guid branchId);
}

public interface IBranchApi
{
    [Get("/api/branches/owner/{ownerId}")]
    Task<IEnumerable<BranchSummary>> GetByOwnerAsync(Guid ownerId);
}

// Request Models
public record LoginRequest(string Cpf, string Password, string CompanyCode);
public record PinLoginRequest(string Cpf, string Pin, string CompanyCode);
public record FingerprintLoginRequest(string CompanyCode, string Cpf, string FingerprintToken);
public record RegisterFingerprintRequest(string CompanyCode, string Cpf, string Password, string FingerprintToken);
public record ChangePasswordRequest(string CompanyCode, string Cpf, string CurrentPassword, string NewPassword);
public record ChangePinRequest(string CompanyCode, string Cpf, string CurrentPassword, string NewPin);
public record RefreshTokenRequest(string RefreshToken);
public record ClockInRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude);
public record ClockOutRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude);
public record BreakRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude);

// Response Models
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    EmployeeInfo Employee,
    Guid BranchId,
    bool IsOwner,
    bool IsHeadquarters);

public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record ClockInResponse(Guid TimeKeepingId);

public record TimeKeepingRecord(
    Guid Id,
    DateOnly WorkDate,
    DateTime? ClockIn,
    DateTime? ClockOut,
    TimeSpan TotalWorked,
    TimeSpan TotalBreak,
    string Status,
    bool IsNocturnal);

public record BranchTimeKeepingRecord(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly WorkDate,
    DateTime? ClockIn,
    DateTime? ClockOut,
    TimeSpan TotalWorked,
    TimeSpan TotalBreak,
    string Status,
    bool IsNocturnal);

public record TimeKeepingSummary(
    Guid EmployeeId,
    string EmployeeName,
    string Status,
    DateTime? LastEntry);

public record EmployeeDetails(
    Guid Id,
    string Name,
    string Cpf,
    string? ImageUrl,
    TimeOnly StartWork,
    TimeOnly EndWork,
    bool IsActive);

public record TimeKeepingDetails(
    string EmployeeName,
    string BranchName,
    string Status,
    DateOnly WorkDate,
    DateTime CreatedAt,
    GeolocationInfo? GeolocationDto,
    IEnumerable<TimeEntryInfo> EntriesDto,
    bool IsNocturnal);

public record GeolocationInfo(double Latitude, double Longitude);

public record TimeEntryInfo(DateTime Timestamp, string Type);

public record BranchSummary(
    Guid Id,
    string Name,
    bool IsActive,
    bool IsHeadquarters);

public record EmployeeSummaryItem(
    Guid EmployeeId,
    string Name,
    string? ImageUrl,
    bool IsActive,
    bool HasFingerprint,
    DocumentInfo Document);

public record DocumentInfo(string Value, string Type);

