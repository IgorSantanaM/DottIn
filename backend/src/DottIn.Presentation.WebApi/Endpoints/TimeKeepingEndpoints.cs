using DottIn.Application.Features.TimeKeepings.Commands.Break;
using DottIn.Application.Features.TimeKeepings.Commands.ClockIn;
using DottIn.Application.Features.TimeKeepings.Commands.ClockOut;
using DottIn.Application.Features.TimeKeepings.DTOs;
using DottIn.Application.Features.TimeKeepings.Queries.GetCurrentTimeKeeping;
using DottIn.Domain.TimeKeepings;
using DottIn.Application.Features.TimeKeepings.Queries.GetTimeKeepingById;
using DottIn.Application.Features.TimeKeepings.Queries.GetTimeKeepingByPeriod;
using DottIn.Application.Features.TimeKeepings.Queries.GetBranchTimeKeepingByPeriod;
using DottIn.Application.Features.TimeKeepings.Queries.GetPagedBranchTimeKeeping;
using DottIn.Domain.Common;
using DottIn.Application.Shared.DTOS;
using DottIn.Presentation.WebApi.DTOs.TimeKeepings;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using DottIn.Presentation.WebApi.Security;

namespace DottIn.Presentation.WebApi.Endpoints
{
    public class TimeKeepingEndpoints : IEndpoint
    {
        private const string Tag = "TimeKeeping";

        public static void DefineEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/timekeeping")
                .WithTags(Tag)
                .RequireAuthorization()
                .AddEndpointFilter<TenantAuthorizationFilter>();

            group.MapGet("/branch/{branchId:guid}/current", HandleGetCurrentTimeKeepingAsync)
                .WithName(nameof(HandleGetCurrentTimeKeepingAsync))
                .WithSummary("Get current time keeping records")
                .WithDescription("Returns all active time keeping records for today in a specific branch.")
                .Produces<IEnumerable<TimeKeepingSummaryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/{timeKeepingId:guid}", HandleGetTimeKeepingByIdAsync)
                .WithName(nameof(HandleGetTimeKeepingByIdAsync))
                .WithSummary("Get time keeping by ID")
                .WithDescription("Returns detailed information about a specific time keeping record.")
                .Produces<TimeKeepingDetailsDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/employee/{employeeId:guid}/history", HandleGetTimeKeepingByPeriodAsync)
                .WithName(nameof(HandleGetTimeKeepingByPeriodAsync))
                .WithSummary("Get employee time keeping history")
                .WithDescription("Returns time keeping records for an employee within a date range.")
                .Produces<IEnumerable<TimeKeepingRecordDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/branch/{branchId:guid}/history/paged", HandleGetPagedBranchTimeKeepingAsync)
                .WithName(nameof(HandleGetPagedBranchTimeKeepingAsync))
                .WithSummary("Get paged branch time keeping history")
                .WithDescription("Returns one page of time keeping records for a branch within a date range.")
                .Produces<PagedResult<BranchTimeKeepingRecordDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status422UnprocessableEntity)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/branch/{branchId:guid}/history", HandleGetBranchTimeKeepingByPeriodAsync)
                .WithName(nameof(HandleGetBranchTimeKeepingByPeriodAsync))
                .WithSummary("Get branch time keeping history")
                .WithDescription("Returns time keeping records for all employees in a branch within a date range.")
                .Produces<IEnumerable<BranchTimeKeepingRecordDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPost("/clock-in", HandleClockInAsync)
                .WithName(nameof(HandleClockInAsync))
                .WithSummary("Clock in")
                .WithDescription("Records an employee clock-in with geolocation verification.")
                .Produces<ClockInResponse>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status422UnprocessableEntity)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPost("/clock-out", HandleClockOutAsync)
                .WithName(nameof(HandleClockOutAsync))
                .WithSummary("Clock out")
                .WithDescription("Records an employee clock-out with geolocation verification.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPost("/break", HandleBreakAsync)
                .WithName(nameof(HandleBreakAsync))
                .WithSummary("Start or end break")
                .WithDescription("Toggles break status for an employee (start break if working, end break if on break).")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        #region Query Handlers

        private static async Task<IResult> HandleGetCurrentTimeKeepingAsync(
            [FromRoute] Guid branchId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetCurrentTimeKeepingQuery(branchId);
            var records = await mediator.Send(query, cancellationToken);
            return Results.Ok(records);
        }

        private static async Task<IResult> HandleGetTimeKeepingByIdAsync(
            [FromRoute] Guid timeKeepingId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetTimeKeepingByIdQuery(timeKeepingId);
            var record = await mediator.Send(query, cancellationToken);

            return record is null
                ? Results.NotFound($"Time keeping record with ID {timeKeepingId} not found.")
                : Results.Ok(record);
        }

        private static async Task<IResult> HandleGetTimeKeepingByPeriodAsync(
            [FromRoute] Guid employeeId,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly? endDate,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetTimeKeepingByPeriodQuery(employeeId, startDate, endDate);
            var records = await mediator.Send(query, cancellationToken);
            return Results.Ok(records);
        }

        private static async Task<IResult> HandleGetPagedBranchTimeKeepingAsync(
            [FromRoute] Guid branchId,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly? endDate,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetPagedBranchTimeKeepingQuery(
                branchId, startDate, endDate, pageNumber, pageSize);
            var page = await mediator.Send(query, cancellationToken);
            return Results.Ok(page);
        }

        private static async Task<IResult> HandleGetBranchTimeKeepingByPeriodAsync(
            [FromRoute] Guid branchId,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly? endDate,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetBranchTimeKeepingByPeriodQuery(branchId, startDate, endDate);
            var records = await mediator.Send(query, cancellationToken);
            return Results.Ok(records);
        }

        #endregion

        #region Command Handlers

        private static async Task<IResult> HandleClockInAsync(
            [FromBody] ClockInRequest request,
            [FromServices] IMediator mediator,
            [FromServices] CurrentUserContext currentUser,
            [FromServices] ILogger<TimeKeepingEndpoints> logger,
            CancellationToken cancellationToken)
        {
            var requestedSource = Enum.TryParse<ClockSource>(request.Source, true, out var s) ? s : ClockSource.Mobile;
            var source = ResolveClockSource(requestedSource, request.EmployeeId, currentUser);
            var skipGeolocation = request.SkipGeolocationValidation && currentUser.IsAdministrator;

            var command = new ClockInCommand(
                request.BranchId,
                request.EmployeeId,
                new GeolocationDto(
                    request.Latitude, request.Longitude, request.AccuracyMeters, request.CapturedAtUtc),
                skipGeolocation,
                source);

            var timeKeepingId = await mediator.Send(command, cancellationToken);

            logger.LogInformation(
                "Clock-in recorded. BranchId: {BranchId}; EmployeeId: {EmployeeId}; Source: {Source}; SkipGeolocation: {SkipGeolocation}",
                request.BranchId, request.EmployeeId, source, skipGeolocation);

            return timeKeepingId == Guid.Empty
                ? Results.BadRequest("Failed to clock in.")
                : Results.Created($"/api/timekeeping/{timeKeepingId}", new ClockInResponse(timeKeepingId));
        }

        private static async Task<IResult> HandleClockOutAsync(
            [FromBody] ClockOutRequest request,
            [FromServices] IMediator mediator,
            [FromServices] CurrentUserContext currentUser,
            [FromServices] ILogger<TimeKeepingEndpoints> logger,
            CancellationToken cancellationToken)
        {
            var requestedSource = Enum.TryParse<ClockSource>(request.Source, true, out var s) ? s : ClockSource.Mobile;
            var source = ResolveClockSource(requestedSource, request.EmployeeId, currentUser);
            var skipGeolocation = request.SkipGeolocationValidation && currentUser.IsAdministrator;

            var command = new ClockOutCommand(
                request.BranchId,
                request.EmployeeId,
                new GeolocationDto(
                    request.Latitude, request.Longitude, request.AccuracyMeters, request.CapturedAtUtc),
                skipGeolocation,
                source);

            await mediator.Send(command, cancellationToken);

            logger.LogInformation(
                "Clock-out recorded. BranchId: {BranchId}; EmployeeId: {EmployeeId}; Source: {Source}; SkipGeolocation: {SkipGeolocation}",
                request.BranchId, request.EmployeeId, source, skipGeolocation);

            return Results.NoContent();
        }

        private static async Task<IResult> HandleBreakAsync(
            [FromBody] BreakRequest request,
            [FromServices] IMediator mediator,
            [FromServices] CurrentUserContext currentUser,
            [FromServices] ILogger<TimeKeepingEndpoints> logger,
            CancellationToken cancellationToken)
        {
            var requestedSource = Enum.TryParse<ClockSource>(request.Source, true, out var s) ? s : ClockSource.Mobile;
            var source = ResolveClockSource(requestedSource, request.EmployeeId, currentUser);
            var skipGeolocation = request.SkipGeolocationValidation && currentUser.IsAdministrator;
            var command = new BreakCommand(
                request.EmployeeId,
                request.BranchId,
                new GeolocationDto(
                    request.Latitude, request.Longitude, request.AccuracyMeters, request.CapturedAtUtc),
                skipGeolocation,
                source);

            await mediator.Send(command, cancellationToken);

            logger.LogInformation(
                "Break action recorded. BranchId: {BranchId}; EmployeeId: {EmployeeId}; Source: {Source}; SkipGeolocation: {SkipGeolocation}",
                request.BranchId, request.EmployeeId, source, skipGeolocation);

            return Results.NoContent();
        }

        private static ClockSource ResolveClockSource(
            ClockSource requestedSource,
            Guid employeeId,
            CurrentUserContext currentUser)
        {
            if (!currentUser.IsManager)
                return ClockSource.Mobile;
            if (employeeId != currentUser.EmployeeId)
                return ClockSource.Kiosk;
            return requestedSource;
        }

        #endregion
    }
}
