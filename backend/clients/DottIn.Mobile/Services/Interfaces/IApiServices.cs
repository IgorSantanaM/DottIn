using Refit;

namespace DottIn.Mobile.Services.Interfaces;

public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request);

    [Post("/api/auth/login/pin")]
    Task<LoginResponse> LoginWithPinAsync([Body] PinLoginRequest request);

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

    [Get("/api/timekeeping/branch/{branchId}/current")]
    Task<IEnumerable<TimeKeepingSummary>> GetCurrentAsync(Guid branchId);
}

public interface IEmployeeApi
{
    [Get("/api/branches/{branchId}/employees/{employeeId}")]
    Task<EmployeeDetails> GetByIdAsync(Guid branchId, Guid employeeId);
}

// Request Models
public record LoginRequest(string Cpf, string Password, string CompanyCode);
public record PinLoginRequest(string Cpf, string Pin, string CompanyCode);
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
    Guid BranchId);

public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record ClockInResponse(Guid TimeKeepingId);

public record TimeKeepingRecord(
    Guid Id,
    DateOnly WorkDate,
    DateTime? ClockIn,
    DateTime? ClockOut,
    TimeSpan TotalWorked,
    TimeSpan TotalBreak,
    string Status);

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
