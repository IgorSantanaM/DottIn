using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Shared.DTOS;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;

namespace DottIn.Presentation.WebApi.DTOs.Settings;

public record PersonalSettings(Guid Id, string Name, string Cpf, string? ImageUrl, string Version)
{
    public static PersonalSettings From(Employee employee) => new(employee.Id, employee.Name,
        employee.CPF.Value, employee.ImageUrl,
        SettingsVersion.For(new { employee.Id, employee.Name, employee.ImageUrl, UpdatedAt = SettingsVersion.Timestamp(employee.UpdatedAt) }));
}
public record UpdatePersonalSettings(string Name, string Version);
public record ChangeSettingsCredential(string CurrentPassword, string NewValue);
public record UnitSettings(Guid Id, string Name, string Document, string CompanyCode,
    string? Email, string? PhoneNumber, AddressDto Address, GeolocationDto Location,
    string TimeZoneId, TimeOnly StartWork, TimeOnly EndWork, int AllowedRadiusMeters,
    int ToleranceMinutes, Guid? HolidayCalendarId, string Version)
{
    public static UnitSettings From(Branch branch)
    {
        var address = branch.Address;
        var settings = new UnitSettings(branch.Id, branch.Name, branch.Document.Value, branch.CompanyCode,
            branch.Email, branch.PhoneNumber,
            new AddressDto(address.Street, address.Number, address.Complement, address.City, address.State, address.ZipCode),
            new GeolocationDto(branch.Location.Latitude, branch.Location.Longitude),
            branch.TimeZoneId, branch.StartWorkTime, branch.EndWorkTime, branch.AllowedRadiusMeters,
            branch.ToleranceMinutes, branch.HolidayCalendarId, "");
        return settings with { Version = SettingsVersion.For(new { settings, UpdatedAt = SettingsVersion.Timestamp(branch.UpdatedAt) }) };
    }
}
public record UpdateUnitSettings(string Section, string Version, AddressDto? Address = null,
    GeolocationDto? Location = null, string? TimeZoneId = null, TimeOnly? StartWork = null,
    TimeOnly? EndWork = null, int? AllowedRadiusMeters = null, int? ToleranceMinutes = null,
    Guid? HolidayCalendarId = null);

public static class SettingsVersion
{
    // PostgreSQL timestamps have microsecond precision; compare the persisted precision.
    public static DateTime? Timestamp(DateTime? value) => value.HasValue
        ? new DateTime(value.Value.Ticks - value.Value.Ticks % 10, DateTimeKind.Utc) : null;
    public static string For<T>(T value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
}
