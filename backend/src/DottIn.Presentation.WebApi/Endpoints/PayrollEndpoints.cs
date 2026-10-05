using DottIn.Application.Features.Payrolls;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.Payrolls;
using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Endpoints;

public sealed class PayrollEndpoints : IEndpoint
{
    public static void DefineEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/payrolls").WithTags("Payroll").RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPost("/{id:guid}/calculate", CalculateAsync);
        group.MapPut("/{id:guid}/employees/{employeeId:guid}/payment", UpdatePaymentAsync);
        group.MapPost("/{id:guid}/close", CloseAsync);
        group.MapPost("/{id:guid}/export", ExportAsync);
        group.MapGet("/accountant-access/{branchId:guid}", ListAccountantAccessAsync);
        group.MapDelete("/accountant-access/{branchId:guid}/{employeeId:guid}", RevokeAccountantAccessAsync);
    }

    private static async Task<IResult> ListAccountantAccessAsync(Guid branchId,
        PayrollAccessService access, DottInContext db, CancellationToken token)
    {
        if (!await access.CanManageAsync(branchId, token)) return Results.NotFound();
        var accountants = await db.AccountantBranchAccesses.AsNoTracking()
            .Where(a => a.BranchId == branchId)
            .Join(db.Employees.AsNoTracking(), a => a.AccountantEmployeeId, e => e.Id,
                (a, e) => new AccountantAccessDto(e.Id, e.Name, a.GrantedAt))
            .OrderBy(a => a.Name).ToListAsync(token);
        return Results.Ok(accountants);
    }

    private static async Task<IResult> RevokeAccountantAccessAsync(Guid branchId, Guid employeeId,
        PayrollAccessService access, DottInContext db, CancellationToken token)
    {
        if (!await access.CanManageAsync(branchId, token)) return Results.NotFound();
        var grant = await db.AccountantBranchAccesses.FirstOrDefaultAsync(a =>
            a.BranchId == branchId && a.AccountantEmployeeId == employeeId, token);
        if (grant is null) return Results.NotFound();
        db.AccountantBranchAccesses.Remove(grant);
        await db.SaveChangesAsync(token);
        return Results.NoContent();
    }

    private static async Task<IResult> ListAsync(
        PayrollAccessService access, CurrentUserContext user, DottInContext db, CancellationToken token)
    {
        var branches = await access.VisibleBranchIdsAsync(token);
        var payrolls = await db.Payrolls.AsNoTracking()
            .Where(p => branches.Contains(p.BranchId) &&
                (user.Role != EmployeeRole.Accountant || p.Status != PayrollStatus.Draft))
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new PayrollListDto(p.Id, p.BranchId, p.Year, p.Month, p.Status,
                db.Branches.Where(b => b.Id == p.BranchId).Select(b => b.Name).FirstOrDefault() ?? "",
                db.PayrollItems.Count(i => i.PayrollId == p.Id),
                db.PayrollItems.Where(i => i.PayrollId == p.Id).Sum(i => i.PaymentAmount) ?? 0m))
            .ToListAsync(token);
        return Results.Ok(payrolls);
    }

    private static async Task<IResult> GetAsync(
        Guid id, PayrollAccessService access, CurrentUserContext user, DottInContext db, CancellationToken token)
    {
        var payroll = await db.Payrolls.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, token);
        if (payroll is null) return Results.NotFound();
        if (!await access.CanViewAsync(payroll.BranchId, token) ||
            user.Role == EmployeeRole.Accountant && payroll.Status == PayrollStatus.Draft)
            return Results.NotFound();
        return Results.Ok(await ToDetailsAsync(db, payroll, token));
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreatePayrollRequest request, PayrollAccessService access,
        CurrentUserContext user, DottInContext db, ITimeKeepingRepository timeKeepings,
        ITimeKeepingAdjustmentRepository adjustments, CancellationToken token)
    {
        if (request.Year is < 2000 or > 2100 || request.Month is < 1 or > 12)
            return Results.BadRequest(new { Message = "Competência inválida." });
        if (!await access.CanManageAsync(request.BranchId, token)) return Results.Forbid();
        if (await db.Payrolls.AnyAsync(p => p.BranchId == request.BranchId &&
            p.Year == request.Year && p.Month == request.Month, token))
            return Results.Conflict(new { Message = "Já existe uma folha para esta filial e competência." });

        var payroll = new Payroll(request.BranchId, request.Year, request.Month, user.EmployeeId, DateTime.UtcNow);
        db.Payrolls.Add(payroll);
        await RefreshItemsAsync(db, timeKeepings, adjustments, payroll, token);
        await db.SaveChangesAsync(token);
        return Results.Created($"/api/payrolls/{payroll.Id}", await ToDetailsAsync(db, payroll, token));
    }

    private static async Task<IResult> CalculateAsync(
        Guid id, [FromBody] PayrollVersionRequest request, PayrollAccessService access,
        DottInContext db, ITimeKeepingRepository timeKeepings,
        ITimeKeepingAdjustmentRepository adjustments, CancellationToken token)
    {
        var payroll = await db.Payrolls.FirstOrDefaultAsync(p => p.Id == id, token);
        if (payroll is null) return Results.NotFound();
        if (!await access.CanManageAsync(payroll.BranchId, token)) return Results.NotFound();
        if (payroll.ConcurrencyToken != request.Version)
            return Results.Conflict(new { Message = "A folha foi alterada. Atualize antes de continuar." });
        payroll.EnsureDraft();
        await RefreshItemsAsync(db, timeKeepings, adjustments, payroll, token);
        payroll.Touch();
        await db.SaveChangesAsync(token);
        return Results.Ok(await ToDetailsAsync(db, payroll, token));
    }

    private static async Task<IResult> UpdatePaymentAsync(
        Guid id, Guid employeeId, [FromBody] SetPayrollPaymentRequest request,
        PayrollAccessService access, CurrentUserContext user, DottInContext db, CancellationToken token)
    {
        var payroll = await db.Payrolls.FirstOrDefaultAsync(p => p.Id == id, token);
        if (payroll is null) return Results.NotFound();
        if (!await access.CanManageAsync(payroll.BranchId, token)) return Results.NotFound();
        if (payroll.ConcurrencyToken != request.Version)
            return Results.Conflict(new { Message = "A folha foi alterada. Atualize antes de continuar." });
        payroll.EnsureDraft();
        var item = await db.PayrollItems.FirstOrDefaultAsync(i => i.PayrollId == id && i.EmployeeId == employeeId, token);
        if (item is null) return Results.NotFound();
        var previous = item.PaymentAmount;
        item.SetPayment(request.Amount, request.Notes);
        db.PayrollPaymentChanges.Add(new PayrollPaymentChange(id, employeeId, user.EmployeeId,
            previous, request.Amount, DateTime.UtcNow));
        payroll.Touch();
        await db.SaveChangesAsync(token);
        return Results.Ok(await ToDetailsAsync(db, payroll, token));
    }

    private static async Task<IResult> CloseAsync(
        Guid id, [FromBody] PayrollVersionRequest request, PayrollAccessService access,
        CurrentUserContext user, DottInContext db, ITimeKeepingRepository timeKeepings,
        ITimeKeepingAdjustmentRepository adjustments, CancellationToken token)
    {
        var payroll = await db.Payrolls.FirstOrDefaultAsync(p => p.Id == id, token);
        if (payroll is null) return Results.NotFound();
        if (!await access.CanManageAsync(payroll.BranchId, token)) return Results.NotFound();
        if (payroll.ConcurrencyToken != request.Version)
            return Results.Conflict(new { Message = "A folha foi alterada. Atualize antes de continuar." });
        payroll.EnsureDraft();
        var items = await RefreshItemsAsync(db, timeKeepings, adjustments, payroll, token);
        payroll.Close(user.EmployeeId, DateTime.UtcNow, items);
        await db.SaveChangesAsync(token);
        return Results.Ok(await ToDetailsAsync(db, payroll, token));
    }

    private static async Task<IResult> ExportAsync(
        Guid id, PayrollAccessService access, CurrentUserContext user,
        DottInContext db, CancellationToken token)
    {
        var payroll = await db.Payrolls.FirstOrDefaultAsync(p => p.Id == id, token);
        if (payroll is null) return Results.NotFound();
        if (!await access.CanExportAsync(payroll.BranchId, token)) return Results.NotFound();
        var items = await db.PayrollItems.AsNoTracking().Where(i => i.PayrollId == id).ToListAsync(token);
        var bytes = PayrollCsvExporter.Export(payroll, items);
        var now = DateTime.UtcNow;
        payroll.MarkExported(user.EmployeeId, now);
        db.PayrollExportEvents.Add(new PayrollExportEvent(id, user.EmployeeId, now));
        await db.SaveChangesAsync(token);
        return Results.File(bytes, "text/csv; charset=utf-8", $"folha-{payroll.Year:D4}-{payroll.Month:D2}.csv");
    }

    private static async Task<List<PayrollItem>> RefreshItemsAsync(
        DottInContext db, ITimeKeepingRepository timeKeepings,
        ITimeKeepingAdjustmentRepository adjustments, Payroll payroll, CancellationToken token)
    {
        var start = new DateOnly(payroll.Year, payroll.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var timeZoneId = await db.Branches.AsNoTracking()
            .Where(b => b.Id == payroll.BranchId).Select(b => b.TimeZoneId).SingleAsync(token);
        var nextMonthLocal = start.AddMonths(1).ToDateTime(TimeOnly.MinValue);
        var endExclusiveUtc = TimeZoneInfo.ConvertTimeToUtc(nextMonthLocal, BranchTime.Resolve(timeZoneId));
        var records = (await timeKeepings.GetByBranchAndPeriodAsync(payroll.BranchId, start, end, token)).ToList();
        var approved = await adjustments.GetApprovedByTimeKeepingIdsAsync(records.Select(x => x.Id), token);
        var hours = PayrollHoursCalculator.Calculate(payroll.BranchId, payroll.Year, payroll.Month, records, approved);
        var recordedEmployeeIds = hours.Keys.ToArray();
        var employees = await db.Employees.AsNoTracking()
            .Where(e => e.BranchId == payroll.BranchId && e.Role != EmployeeRole.Owner &&
                e.Role != EmployeeRole.Accountant &&
                ((e.IsActive && e.CreatedAt < endExclusiveUtc) || recordedEmployeeIds.Contains(e.Id)))
            .ToListAsync(token);
        var codes = await db.DominioEmployeeMappings.AsNoTracking()
            .Where(m => m.BranchId == payroll.BranchId)
            .ToDictionaryAsync(m => m.EmployeeId, m => m.DominioCode, token);
        var items = await db.PayrollItems.Where(i => i.PayrollId == payroll.Id).ToListAsync(token);
        var known = items.ToDictionary(i => i.EmployeeId);
        foreach (var employee in employees)
        {
            hours.TryGetValue(employee.Id, out var calculated);
            codes.TryGetValue(employee.Id, out var code);
            if (known.TryGetValue(employee.Id, out var item))
                item.SetSnapshot(employee.Name, code, calculated?.WorkedMinutes ?? 0,
                    calculated?.HasIncompleteRecords ?? false);
            else
            {
                item = new PayrollItem(payroll.Id, employee.Id, employee.Name, code,
                    calculated?.WorkedMinutes ?? 0, calculated?.HasIncompleteRecords ?? false);
                db.PayrollItems.Add(item);
                items.Add(item);
            }
        }
        return items;
    }

    private static async Task<PayrollDetailsDto> ToDetailsAsync(DottInContext db, Payroll payroll, CancellationToken token)
    {
        var items = await db.PayrollItems.AsNoTracking().Where(i => i.PayrollId == payroll.Id)
            .OrderBy(i => i.EmployeeName).ToListAsync(token);
        return new PayrollDetailsDto(payroll.Id, payroll.BranchId, payroll.Year, payroll.Month,
            payroll.Status, payroll.ConcurrencyToken, payroll.ClosedAt,
            items.Select(i => new PayrollItemDto(i.EmployeeId, i.EmployeeName, i.DominioCode,
                i.WorkedMinutes, i.HasIncompleteRecords, i.CalculatedAmount, i.PaymentAmount,
                i.Notes)).ToList(), items.Sum(i => i.PaymentAmount ?? 0m));
    }
}

public sealed record CreatePayrollRequest(Guid BranchId, int Year, int Month);
public sealed record PayrollVersionRequest(Guid Version);
public sealed record SetPayrollPaymentRequest(decimal Amount, string? Notes, Guid Version);
public sealed record PayrollListDto(Guid Id, Guid BranchId, int Year, int Month,
    PayrollStatus Status, string BranchName, int EmployeeCount, decimal TotalAmount);
public sealed record PayrollItemDto(Guid EmployeeId, string EmployeeName, string? DominioCode,
    long WorkedMinutes, bool HasIncompleteRecords, decimal? CalculatedAmount,
    decimal? PaymentAmount, string? Notes);
public sealed record PayrollDetailsDto(Guid Id, Guid BranchId, int Year, int Month,
    PayrollStatus Status, Guid Version, DateTime? ClosedAt,
    IReadOnlyList<PayrollItemDto> Items, decimal TotalAmount);
public sealed record AccountantAccessDto(Guid EmployeeId, string Name, DateTime GrantedAt);
