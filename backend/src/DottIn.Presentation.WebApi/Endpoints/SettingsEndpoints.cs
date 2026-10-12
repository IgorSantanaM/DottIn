using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.ValueObjects;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.DTOs.Settings;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Presentation.WebApi.Security;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DottIn.Presentation.WebApi.Endpoints;

/// <summary>Personal mutations use the authenticated identity, never a client-supplied employee ID.</summary>
public sealed class SettingsEndpoints : IEndpoint
{
    public static void DefineEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings").RequireAuthorization();
        group.MapGet("/me", GetPersonalAsync);
        group.MapPut("/me", UpdatePersonalAsync);
        group.MapPut("/password", ChangePasswordAsync).RequireRateLimiting("public-auth");
        group.MapPut("/pin", ChangePinAsync).RequireRateLimiting("public-auth");
        group.MapGet("/branches/{branchId:guid}", GetUnitAsync);
        group.MapPut("/branches/{branchId:guid}", UpdateUnitAsync);
    }

    private static Task<Employee?> ActiveEmployeeAsync(DottInContext db, CurrentUserContext user, CancellationToken ct)
        => db.Employees.FirstOrDefaultAsync(e => e.Id == user.EmployeeId && e.IsActive, ct);

    private static async Task<IResult> GetPersonalAsync(DottInContext db, CurrentUserContext user, CancellationToken ct)
    {
        var employee = await ActiveEmployeeAsync(db, user, ct);
        return employee is null ? Results.Unauthorized() : Results.Ok(PersonalSettings.From(employee));
    }

    private static async Task<IResult> UpdatePersonalAsync(UpdatePersonalSettings request,
        DottInContext db, CurrentUserContext user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 150)
            return BadRequest("Informe um nome com até 150 caracteres.");
        return await InTransactionAsync(db, ct, async () =>
        {
            var employee = await db.Employees.FromSqlInterpolated(
                $"SELECT * FROM \"Employees\" WHERE \"Id\" = {user.EmployeeId} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (employee is null || !employee.IsActive) return Results.Unauthorized();
            if (PersonalSettings.From(employee).Version != request.Version) return Conflict();
            employee.UpdateProfile(request.Name.Trim());
            await db.SaveChangesAsync(ct);
            return Results.Ok(PersonalSettings.From(employee));
        });
    }

    private static Task<IResult> ChangePasswordAsync(ChangeSettingsCredential request, DottInContext db,
        CurrentUserContext user, HttpContext http, CancellationToken ct)
        => ChangeCredentialAsync(request, db, user, http, false, ct);

    private static Task<IResult> ChangePinAsync(ChangeSettingsCredential request, DottInContext db,
        CurrentUserContext user, HttpContext http, CancellationToken ct)
        => ChangeCredentialAsync(request, db, user, http, true, ct);

    private static async Task<IResult> ChangeCredentialAsync(ChangeSettingsCredential request, DottInContext db,
        CurrentUserContext user, HttpContext http, bool pin, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || request.CurrentPassword.Length > 128)
            return BadRequest("Informe sua senha atual.");
        if (pin ? request.NewValue is null || request.NewValue.Length != 6 || request.NewValue.Any(c => c < '0' || c > '9')
                : string.IsNullOrWhiteSpace(request.NewValue) || request.NewValue.Length is < 10 or > 128 ||
                  !request.NewValue.Any(char.IsUpper) || !request.NewValue.Any(char.IsLower) ||
                  !request.NewValue.Any(char.IsDigit) || request.NewValue.All(char.IsLetterOrDigit))
            return BadRequest(pin ? "O PIN deve conter 6 números." : "Use de 10 a 128 caracteres, com maiúscula, minúscula, número e símbolo.");
        return await InTransactionAsync(db, ct, async () =>
        {
            var employee = await db.Employees.FromSqlInterpolated(
                $"SELECT * FROM \"Employees\" WHERE \"Id\" = {user.EmployeeId} FOR UPDATE").SingleOrDefaultAsync(ct);
            if (employee is null || !employee.IsActive) return Results.Unauthorized();
            // A wrong password is a validation error, not an expired session (401).
            if (!employee.VerifyPassword(request.CurrentPassword)) return BadRequest("A senha atual está incorreta.");
            if (!pin && employee.VerifyPassword(request.NewValue)) return BadRequest("A nova senha deve ser diferente da atual.");
            if (pin) employee.SetPin(request.NewValue);
            else employee.SetPassword(request.NewValue);
            employee.RotateSessionVersion();
            await db.SaveChangesAsync(ct);
            await db.RefreshTokens.Where(t => t.EmployeeId == employee.Id).ExecuteDeleteAsync(ct);
            WebSessionCookie.Delete(http);
            return Results.NoContent();
        });
    }

    private static async Task<bool> CanEditUnitAsync(Guid branchId, DottInContext db, CurrentUserContext user,
        TenantAccessService access, CancellationToken ct)
    {
        if (!user.IsAdministrator) return false;
        var employee = await ActiveEmployeeAsync(db, user, ct);
        return employee?.Role is EmployeeRole.Owner or EmployeeRole.Administrator &&
               await access.CanAccessBranchAsync(branchId, requireAdministrator: true, token: ct);
    }

    private static async Task<IResult> GetUnitAsync(Guid branchId, DottInContext db, CurrentUserContext user,
        TenantAccessService access, CancellationToken ct)
    {
        if (!await CanEditUnitAsync(branchId, db, user, access, ct)) return Results.Forbid();
        var branch = await db.Branches.AsNoTracking().SingleAsync(b => b.Id == branchId, ct);
        return Results.Ok(UnitSettings.From(branch));
    }

    private static async Task<IResult> UpdateUnitAsync(Guid branchId, UpdateUnitSettings request,
        DottInContext db, CurrentUserContext user, TenantAccessService access, CancellationToken ct)
    {
        if (!await CanEditUnitAsync(branchId, db, user, access, ct)) return Results.Forbid();
        if (request.Section is not ("location" or "schedule" or "rules")) return BadRequest("Seção inválida.");
        return await InTransactionAsync(db, ct, async () =>
        {
            var branch = await db.Branches.FromSqlInterpolated(
                $"SELECT * FROM \"Branches\" WHERE \"Id\" = {branchId} FOR UPDATE").SingleAsync(ct);
            if (!await CanEditUnitAsync(branchId, db, user, access, ct)) return Results.Forbid();
            var before = UnitSettings.From(branch);
            if (before.Version != request.Version) return Conflict();
            switch (request.Section)
            {
                case "location":
                    if (request.Address is null || request.Location is null || string.IsNullOrWhiteSpace(request.TimeZoneId))
                        return BadRequest("Informe endereço, coordenadas e fuso horário.");
                    if (!double.IsFinite(request.Location.Latitude) || !double.IsFinite(request.Location.Longitude) ||
                        request.Location.Latitude is < -90 or > 90 || request.Location.Longitude is < -180 or > 180)
                        return BadRequest("Coordenadas inválidas.");
                    var address = request.Address;
                    if (string.IsNullOrWhiteSpace(address.Street) || string.IsNullOrWhiteSpace(address.City) ||
                        address.Street.Length > 200 || address.City.Length > 100 || address.Complement?.Length > 100 || request.TimeZoneId.Length > 100)
                        return BadRequest("O endereço ou fuso excede o tamanho permitido.");
                    branch.MoveLocation(new Address(address.Street, address.Number, address.City, address.State,
                        address.ZipCode, address.Complement), new Geolocation(request.Location.Latitude,
                        request.Location.Longitude), request.TimeZoneId);
                    break;
                case "schedule":
                    if (!request.StartWork.HasValue || !request.EndWork.HasValue || request.StartWork == request.EndWork)
                        return BadRequest("Informe horários de entrada e saída diferentes.");
                    branch.UpdateSchedule(request.StartWork.Value, request.EndWork.Value);
                    break;
                case "rules":
                    if (request.AllowedRadiusMeters is null or < 10 or > 10000 || request.ToleranceMinutes is null or < 5 or > 60)
                        return BadRequest("O raio deve ser de 10 a 10.000 metros e a tolerância de 5 a 60 minutos.");
                    if (request.HolidayCalendarId.HasValue && !await db.HolidayCalendars.AnyAsync(
                        c => c.Id == request.HolidayCalendarId && c.BranchId == branchId && c.IsActive, ct))
                        return BadRequest("Selecione um calendário ativo desta unidade.");
                    branch.UpdateConfig(request.AllowedRadiusMeters.Value, branch.TimeZoneId);
                    branch.SetComplianceRules(request.ToleranceMinutes.Value, request.HolidayCalendarId);
                    break;
            }
            var after = UnitSettings.From(branch);
            db.BranchSettingsAudits.Add(new(branchId, user.EmployeeId, request.Section,
                JsonSerializer.Serialize(before), JsonSerializer.Serialize(after)));
            await db.SaveChangesAsync(ct);
            return Results.Ok(after);
        });
    }

    private static Task<IResult> InTransactionAsync(DottInContext db, CancellationToken ct, Func<Task<IResult>> operation)
        => db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // Retries must reload current values rather than reuse the first attempt's tracked entities.
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await operation();
            await transaction.CommitAsync(ct);
            return result;
        });

    private static IResult Conflict() => Results.Conflict(new { message = "Esses dados foram alterados por outra pessoa. Recarregue a seção antes de salvar novamente." });
    private static IResult BadRequest(string message) => Results.BadRequest(new { message });
}
