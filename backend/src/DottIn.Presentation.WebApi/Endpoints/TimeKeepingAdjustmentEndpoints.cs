using DottIn.Application.Features.TimeKeepings;
using DottIn.Domain.Branches;
using DottIn.Domain.Core.Data;
using DottIn.Domain.TimeKeepings;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.DTOs.TimeKeepings;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Endpoints;

public sealed class TimeKeepingAdjustmentEndpoints : IEndpoint
{
    private const string Tag = "Time keeping adjustments";

    public static void DefineEndpoints(WebApplication app)
    {
        app.MapPost("/api/timekeeping/{timeKeepingId:guid}/adjustments", HandleCreateAsync)
            .WithTags(Tag)
            .RequireAuthorization()
            .AddEndpointFilter<TenantAuthorizationFilter>()
            .Produces<TimeKeepingAdjustmentResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        var managerGroup = app.MapGroup("/api/branches/{branchId:guid}/timekeeping-adjustments")
            .WithTags(Tag)
            .RequireAuthorization()
            .AddEndpointFilter<TenantAuthorizationFilter>();

        managerGroup.MapGet("/", HandleListAsync)
            .Produces<IEnumerable<TimeKeepingAdjustmentResponse>>(StatusCodes.Status200OK);

        managerGroup.MapPut("/{adjustmentId:guid}/decision", HandleReviewAsync)
            .Produces<TimeKeepingAdjustmentResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleCreateAsync(
        [FromRoute] Guid timeKeepingId,
        [FromBody] CreateTimeKeepingAdjustmentRequest request,
        [FromServices] CurrentUserContext currentUser,
        [FromServices] ITimeKeepingRepository timeKeepingRepository,
        [FromServices] ITimeKeepingAdjustmentRepository adjustmentRepository,
        [FromServices] DottInContext db,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var timeKeeping = await timeKeepingRepository.GetWithEntriesByIdAsync(timeKeepingId, cancellationToken);
        if (timeKeeping is null)
            return Results.NotFound();

        var proposedTimestamp = ToUtc(request.ProposedTimestamp, timeKeeping.TimeZoneId);
        var originalTimestamp = request.OriginalTimestamp.HasValue
            ? ToUtc(request.OriginalTimestamp.Value, timeKeeping.TimeZoneId)
            : (DateTime?)null;

        if (originalTimestamp.HasValue && !timeKeeping.Entries.Any(entry =>
                entry.Type == request.EntryType && entry.Timestamp == originalTimestamp.Value))
        {
            return Results.BadRequest(new { Message = "A marcação original não foi encontrada nesta jornada." });
        }

        var localDate = BranchTime.GetLocalDate(proposedTimestamp, timeKeeping.TimeZoneId);
        if (localDate < timeKeeping.WorkDate.AddDays(-1) || localDate > timeKeeping.WorkDate.AddDays(1))
            return Results.BadRequest(new { Message = "O horário proposto está fora do período permitido para esta jornada." });

        if (await adjustmentRepository.HasPendingForEntryAsync(
                timeKeepingId, request.EntryType, originalTimestamp, cancellationToken))
        {
            return Results.Conflict(new { Message = "Já existe uma correção pendente para esta marcação." });
        }

        var adjustment = new TimeKeepingAdjustment(
            timeKeeping.Id,
            timeKeeping.BranchId,
            timeKeeping.EmployeeId,
            currentUser.EmployeeId,
            request.EntryType,
            originalTimestamp,
            proposedTimestamp,
            request.Reason,
            DateTime.UtcNow);

        await adjustmentRepository.AddAsync(adjustment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var names = await LoadEmployeeNamesAsync(db, [adjustment.EmployeeId, adjustment.RequestedByEmployeeId], cancellationToken);

        return Results.Created(
            $"/api/branches/{timeKeeping.BranchId}/timekeeping-adjustments/{adjustment.Id}",
            ToResponse(adjustment, names));
    }

    private static async Task<IResult> HandleListAsync(
        [FromRoute] Guid branchId,
        [FromQuery] TimeKeepingAdjustmentStatus? status,
        [FromServices] ITimeKeepingAdjustmentRepository adjustmentRepository,
        [FromServices] DottInContext db,
        CancellationToken cancellationToken)
    {
        var adjustments = (await adjustmentRepository.GetByBranchAsync(branchId, status, cancellationToken)).ToList();
        var employeeIds = adjustments
            .SelectMany(x => new Guid?[] { x.EmployeeId, x.RequestedByEmployeeId, x.ReviewedByEmployeeId })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct();
        var names = await LoadEmployeeNamesAsync(db, employeeIds, cancellationToken);
        return Results.Ok(adjustments.Select(x => ToResponse(x, names)));
    }

    private static async Task<IResult> HandleReviewAsync(
        [FromRoute] Guid branchId,
        [FromRoute] Guid adjustmentId,
        [FromBody] ReviewTimeKeepingAdjustmentRequest request,
        [FromServices] CurrentUserContext currentUser,
        [FromServices] ITimeKeepingRepository timeKeepingRepository,
        [FromServices] ITimeKeepingAdjustmentRepository adjustmentRepository,
        [FromServices] DottInContext db,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var adjustment = await adjustmentRepository.GetByIdAsync(adjustmentId, cancellationToken);
        if (adjustment is null || adjustment.BranchId != branchId)
            return Results.NotFound();

        var now = DateTime.UtcNow;
        if (request.Approve)
        {
            adjustment.Approve(currentUser.EmployeeId, now, request.ReviewNote);
            var timeKeeping = await timeKeepingRepository.GetWithEntriesByIdAsync(adjustment.TimeKeepingId, cancellationToken);
            if (timeKeeping is null)
                return Results.NotFound();

            var approved = await adjustmentRepository.GetApprovedByTimeKeepingIdsAsync(
                [adjustment.TimeKeepingId], cancellationToken);
            if (!TimeKeepingMetricsCalculator.HasValidSequence(timeKeeping, approved.Append(adjustment)))
            {
                return Results.BadRequest(new
                {
                    Message = "A correção produziria uma sequência inválida de entrada, intervalo ou saída."
                });
            }
        }
        else
        {
            adjustment.Reject(currentUser.EmployeeId, now, request.ReviewNote);
        }

        await adjustmentRepository.UpdateAsync(adjustment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var names = await LoadEmployeeNamesAsync(
            db,
            [adjustment.EmployeeId, adjustment.RequestedByEmployeeId, currentUser.EmployeeId],
            cancellationToken);
        return Results.Ok(ToResponse(adjustment, names));
    }

    private static async Task<Dictionary<Guid, string>> LoadEmployeeNamesAsync(
        DottInContext db,
        IEnumerable<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToArray();
        return await db.Employees.AsNoTracking()
            .Where(employee => ids.Contains(employee.Id))
            .ToDictionaryAsync(employee => employee.Id, employee => employee.Name, cancellationToken);
    }

    private static TimeKeepingAdjustmentResponse ToResponse(
        TimeKeepingAdjustment adjustment,
        IReadOnlyDictionary<Guid, string> names)
        => new(
            adjustment.Id,
            adjustment.TimeKeepingId,
            adjustment.EmployeeId,
            names.GetValueOrDefault(adjustment.EmployeeId, "Funcionário"),
            adjustment.RequestedByEmployeeId,
            names.GetValueOrDefault(adjustment.RequestedByEmployeeId, "Funcionário"),
            adjustment.ReviewedByEmployeeId,
            adjustment.ReviewedByEmployeeId.HasValue
                ? names.GetValueOrDefault(adjustment.ReviewedByEmployeeId.Value, "Gestor")
                : null,
            adjustment.EntryType,
            adjustment.OriginalTimestamp,
            adjustment.ProposedTimestamp,
            adjustment.Reason,
            adjustment.ReviewNote,
            adjustment.Status,
            adjustment.CreatedAt,
            adjustment.ReviewedAt);

    private static DateTime ToUtc(DateTime timestamp, string timeZoneId)
    {
        if (timestamp.Kind == DateTimeKind.Utc)
            return timestamp;

        var local = DateTime.SpecifyKind(timestamp, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, BranchTime.Resolve(timeZoneId));
    }
}
