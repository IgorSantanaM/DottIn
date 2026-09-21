using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Application.Features.TimeKeepings.Queries.GetBranchTimeKeepingByPeriod;
using DottIn.Application.Features.TimeKeepings.Queries.GetTimeKeepingByPeriod;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using DottIn.Presentation.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DottIn.Presentation.WebApi.Endpoints;

public sealed class DashboardEndpoints : IEndpoint
{
    public static void DefineEndpoints(WebApplication app)
    {
        app.MapGet("/api/branches/{branchId:guid}/dashboard", HandleGetSummaryAsync)
            .WithTags("Dashboard")
            .WithName(nameof(HandleGetSummaryAsync))
            .RequireAuthorization()
            .AddEndpointFilter<TenantAuthorizationFilter>()
            .Produces<DashboardSummaryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> HandleGetSummaryAsync(
        [FromRoute] Guid branchId,
        [FromServices] CurrentUserContext currentUser,
        [FromServices] IBranchRepository branchRepository,
        [FromServices] IEmployeeRepository employeeRepository,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch is null || !branch.IsActive)
            return Results.NotFound();

        var utcNow = DateTime.UtcNow;
        var localNow = BranchTime.ToLocal(utcNow, branch.TimeZoneId);
        var localDate = DateOnly.FromDateTime(localNow);

        if (currentUser.IsOwner)
        {
            var activeEmployees = await employeeRepository.CountActiveByBranchIdAsync(branchId, cancellationToken);
            var records = (await mediator.Send(
                new GetBranchTimeKeepingByPeriodQuery(branchId, localDate, localDate),
                cancellationToken)).ToList();

            return Results.Ok(new DashboardSummaryResponse(
                activeEmployees,
                records,
                null,
                utcNow,
                localNow,
                branch.TimeZoneId));
        }

        var personalRecords = await mediator.Send(
            new GetTimeKeepingByPeriodQuery(currentUser.EmployeeId, localDate, localDate),
            cancellationToken);

        return Results.Ok(new DashboardSummaryResponse(
            0,
            [],
            personalRecords.FirstOrDefault(),
            utcNow,
            localNow,
            branch.TimeZoneId));
    }
}

public sealed record DashboardSummaryResponse(
    int ActiveEmployees,
    IReadOnlyList<BranchTimeKeepingRecordDto> TodayRecords,
    TimeKeepingRecordDto? PersonalRecord,
    DateTime UtcNow,
    DateTime LocalNow,
    string TimeZoneId);