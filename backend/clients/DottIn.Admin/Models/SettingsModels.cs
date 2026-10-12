namespace DottIn.Admin.Models;

public record PersonalSettings(Guid Id, string Name, string Cpf, string? ImageUrl, string Version);
public record UpdatePersonalSettings(string Name, string Version);
public record UnitSettings(Guid Id, string Name, string Document, string CompanyCode,
    string? Email, string? PhoneNumber, BranchAddress Address, BranchCoordinates Location,
    string TimeZoneId, TimeOnly StartWork, TimeOnly EndWork, int AllowedRadiusMeters,
    int ToleranceMinutes, Guid? HolidayCalendarId, string Version);
public record UpdateUnitSettings(string Section, string Version, BranchAddress? Address = null,
    BranchCoordinates? Location = null, string? TimeZoneId = null, TimeOnly? StartWork = null,
    TimeOnly? EndWork = null, int? AllowedRadiusMeters = null, int? ToleranceMinutes = null,
    Guid? HolidayCalendarId = null);
