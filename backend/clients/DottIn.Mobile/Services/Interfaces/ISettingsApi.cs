using Refit;

namespace DottIn.Mobile.Services.Interfaces;

public interface ISettingsApi
{
    [Get("/api/settings/me")]
    Task<PersonalSettings> GetPersonalAsync();
    [Put("/api/settings/me")]
    Task<PersonalSettings> SavePersonalAsync([Body] UpdatePersonalSettings request);
    [Put("/api/settings/password")]
    Task ChangePasswordAsync([Body] ChangeSettingsCredential request);
    [Put("/api/settings/pin")]
    Task ChangePinAsync([Body] ChangeSettingsCredential request);
    [Get("/api/settings/branches/{branchId}")]
    Task<UnitSettings> GetUnitAsync(Guid branchId);
    [Put("/api/settings/branches/{branchId}")]
    Task<UnitSettings> SaveUnitAsync(Guid branchId, [Body] UpdateUnitSettings request);
}
public record PersonalSettings(Guid Id, string Name, string Cpf, string? ImageUrl, string Version);
public record UpdatePersonalSettings(string Name, string Version);
public record ChangeSettingsCredential(string CurrentPassword, string NewValue);
public record UnitSettings(Guid Id, string Name, string Document, string CompanyCode,
    string? Email, string? PhoneNumber, BranchAddress Address, GeolocationInfo Location,
    string TimeZoneId, TimeOnly StartWork, TimeOnly EndWork, int AllowedRadiusMeters,
    int ToleranceMinutes, Guid? HolidayCalendarId, string Version);
public record UpdateUnitSettings(string Section, string Version, BranchAddress? Address = null,
    GeolocationInfo? Location = null, string? TimeZoneId = null, TimeOnly? StartWork = null,
    TimeOnly? EndWork = null, int? AllowedRadiusMeters = null, int? ToleranceMinutes = null,
    Guid? HolidayCalendarId = null);
