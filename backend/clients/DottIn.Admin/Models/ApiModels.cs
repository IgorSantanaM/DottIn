namespace DottIn.Admin.Models;

public record LoginRequest(string Cpf, string Password, string CompanyCode);
public record PinLoginRequest(string Cpf, string Pin, string CompanyCode);
public record ClockInRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude, bool SkipGeolocationValidation = false);
public record ClockOutRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude, bool SkipGeolocationValidation = false);
public record BreakRequest(Guid BranchId, Guid EmployeeId, double Latitude, double Longitude, bool SkipGeolocationValidation = false);
public record ClockInResponse(Guid TimeKeepingId);

public record LoginResponse(
    string AccessToken, string RefreshToken, DateTime ExpiresAt,
    EmployeeInfo Employee, Guid BranchId, bool IsOwner, bool IsHeadquarters);

public record EmployeeInfo(Guid Id, string Name, string Cpf, string? ImageUrl)
{
    public string FirstName => Name.Split(' ').FirstOrDefault() ?? Name;
    public string Initials => string.Join("", Name.Split(' ').Take(2).Select(n => n.FirstOrDefault()));
}

public record BranchSummary(
    Guid Id, string Name, string? Email, string? PhoneNumber,
    bool IsActive, bool IsHeadquarters, string OwnerName);

public record EmployeeSummary(
    Guid EmployeeId, string Name, DocumentInfo Document, string? ImageUrl,
    string BranchName, TimeOnly StartWorkTime, TimeOnly EndWorkTime,
    bool IsActive, bool HasFingerprint);

public record DocumentInfo(string Value, string Type);

public record TimeKeepingRecord(
    Guid Id, Guid EmployeeId, string EmployeeName, DateOnly WorkDate,
    DateTime? ClockIn, DateTime? ClockOut,
    TimeSpan TotalWorked, TimeSpan TotalBreak,
    string Status, bool IsNocturnal);

public record TimeKeepingDetails(
    string EmployeeName, string BranchName, string Status,
    DateOnly WorkDate, DateTime CreatedAt,
    GeolocationInfo? GeolocationDto,
    IEnumerable<TimeEntryInfo> EntriesDto, bool IsNocturnal);

public record GeolocationInfo(double Latitude, double Longitude);
public record TimeEntryInfo(DateTime Timestamp, string Type);

public record ApiProblem(int Status, string? Title, object? Errors);

public class ApiException(string message) : Exception(message);
