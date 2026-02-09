using DottIn.Application.Features.HolidayCalendars.Commands.AddHoliday;
using DottIn.Application.Features.HolidayCalendars.Commands.ClearHolidays;
using DottIn.Application.Features.HolidayCalendars.Commands.CreateHolidayCalendar;
using DottIn.Application.Features.HolidayCalendars.Commands.RemoveHoliday;
using DottIn.Application.Features.HolidayCalendars.Commands.UpdateHoliday;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using DottIn.Application.Features.HolidayCalendars.Queries.GetActiveHolidayCalendars;
using DottIn.Application.Features.HolidayCalendars.Queries.GetAllHolidayCalendars;
using DottIn.Application.Features.HolidayCalendars.Queries.GetHolidayCalendarById;
using DottIn.Application.Features.HolidayCalendars.Queries.GetHolidayCalendarByYear;
using DottIn.Application.Features.HolidayCalendars.Queries.GetHolidaysByDate;
using DottIn.Application.Features.HolidayCalendars.Queries.GetHolidaysInRange;
using DottIn.Domain.HolidayCalendars;
using DottIn.Presentation.WebApi.DTOs.HolidayCalendars;
using DottIn.Presentation.WebApi.Endpoints.Internal;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DottIn.Presentation.WebApi.Endpoints
{
    public class HolidayCalendarEndpoints : IEndpoint
    {
        private const string Tag = "HolidayCalendars";

        public static void DefineEndpoints(WebApplication app)
        {
            var group = app.MapGroup("/api/branches/{branchId:guid}/holiday-calendars")
                .WithTags(Tag);

            group.MapGet("/", HandleGetAllCalendarsAsync)
                .WithName(nameof(HandleGetAllCalendarsAsync))
                .WithSummary("Get all holiday calendars")
                .WithDescription("Returns all holiday calendars for a specific branch.")
                .Produces<IEnumerable<HolidayCalendarSummaryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/active", HandleGetActiveCalendarsAsync)
                .WithName(nameof(HandleGetActiveCalendarsAsync))
                .WithSummary("Get active holiday calendars")
                .WithDescription("Returns all active holiday calendars for a specific branch.")
                .Produces<IEnumerable<HolidayCalendarSummaryDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/{calendarId:guid}", HandleGetCalendarByIdAsync)
                .WithName(nameof(HandleGetCalendarByIdAsync))
                .WithSummary("Get holiday calendar by ID")
                .WithDescription("Returns detailed information about a specific holiday calendar including all holidays.")
                .Produces<HolidayCalendarDetailsDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/year/{year:int}", HandleGetCalendarByYearAsync)
                .WithName(nameof(HandleGetCalendarByYearAsync))
                .WithSummary("Get holiday calendar by year")
                .WithDescription("Returns the active holiday calendar for a specific year.")
                .Produces<HolidayCalendarDetailsDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/holidays/date/{date}", HandleGetHolidaysByDateAsync)
                .WithName(nameof(HandleGetHolidaysByDateAsync))
                .WithSummary("Get holiday by date")
                .WithDescription("Returns holiday information for a specific date if it exists.")
                .Produces<HolidayDto>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapGet("/holidays/range", HandleGetHolidaysInRangeAsync)
                .WithName(nameof(HandleGetHolidaysInRangeAsync))
                .WithSummary("Get holidays in date range")
                .WithDescription("Returns all holidays within a specified date range.")
                .Produces<IEnumerable<HolidayDto>>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPost("/", HandleCreateCalendarAsync)
                .WithName(nameof(HandleCreateCalendarAsync))
                .WithSummary("Create a new holiday calendar")
                .WithDescription("Creates a new holiday calendar for a branch.")
                .Produces<Guid>(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status422UnprocessableEntity)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPost("/{calendarId:guid}/holidays", HandleAddHolidaysAsync)
                .WithName(nameof(HandleAddHolidaysAsync))
                .WithSummary("Add holidays to calendar")
                .WithDescription("Adds one or more holidays to an existing calendar.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapPut("/{calendarId:guid}/holidays/{date}", HandleUpdateHolidayAsync)
                .WithName(nameof(HandleUpdateHolidayAsync))
                .WithSummary("Update a holiday")
                .WithDescription("Updates an existing holiday in a calendar.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapDelete("/{calendarId:guid}/holidays/{date}", HandleRemoveHolidayAsync)
                .WithName(nameof(HandleRemoveHolidayAsync))
                .WithSummary("Remove a holiday")
                .WithDescription("Removes a holiday from a calendar.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);

            group.MapDelete("/{calendarId:guid}/holidays", HandleClearHolidaysAsync)
                .WithName(nameof(HandleClearHolidaysAsync))
                .WithSummary("Clear all holidays")
                .WithDescription("Removes all holidays from a calendar.")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status500InternalServerError);
        }

        #region Query Handlers

        private static async Task<IResult> HandleGetAllCalendarsAsync(
            [FromRoute] Guid branchId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetAllHolidayCalendarsQuery(branchId);
            var calendars = await mediator.Send(query, cancellationToken);
            return Results.Ok(calendars);
        }

        private static async Task<IResult> HandleGetActiveCalendarsAsync(
            [FromRoute] Guid branchId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetActiveHolidayCalendarsQuery(branchId);
            var calendars = await mediator.Send(query, cancellationToken);
            return Results.Ok(calendars);
        }

        private static async Task<IResult> HandleGetCalendarByIdAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid calendarId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetHolidayCalendarByIdQuery(branchId, calendarId);
            var calendar = await mediator.Send(query, cancellationToken);

            return calendar is null
                ? Results.NotFound($"Holiday calendar with ID {calendarId} not found.")
                : Results.Ok(calendar);
        }

        private static async Task<IResult> HandleGetCalendarByYearAsync(
            [FromRoute] Guid branchId,
            [FromRoute] int year,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetHolidayCalendarByYearQuery(branchId, year);
            var calendar = await mediator.Send(query, cancellationToken);

            return calendar is null
                ? Results.NotFound($"Holiday calendar for year {year} not found.")
                : Results.Ok(calendar);
        }

        private static async Task<IResult> HandleGetHolidaysByDateAsync(
            [FromRoute] Guid branchId,
            [FromRoute] DateOnly date,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetHolidaysByDateQuery(branchId, date);
            var holiday = await mediator.Send(query, cancellationToken);

            return holiday is null
                ? Results.NotFound($"No holiday found for date {date:dd/MM/yyyy}.")
                : Results.Ok(holiday);
        }

        private static async Task<IResult> HandleGetHolidaysInRangeAsync(
            [FromRoute] Guid branchId,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetHolidaysInRangeQuery(branchId, startDate, endDate);
            var holidays = await mediator.Send(query, cancellationToken);
            return Results.Ok(holidays);
        }

        #endregion

        #region Command Handlers

        private static async Task<IResult> HandleCreateCalendarAsync(
            [FromRoute] Guid branchId,
            [FromBody] CreateHolidayCalendarRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new CreateHolidayCalendarCommand(
                branchId,
                request.Name,
                request.CountryCode,
                request.Year,
                request.RegionCode,
                request.Description);

            var calendarId = await mediator.Send(command, cancellationToken);

            return calendarId == Guid.Empty
                ? Results.BadRequest("Failed to create holiday calendar.")
                : Results.CreatedAtRoute(nameof(HandleGetCalendarByIdAsync), new { branchId, calendarId }, calendarId);
        }

        private static async Task<IResult> HandleAddHolidaysAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid calendarId,
            [FromBody] AddHolidaysRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var holidays = request.Holidays
                .Select(h => (h.Date, h.Name, h.Type, h.IsOptional));

            var command = new AddHolidayCommand(calendarId, branchId, holidays);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleUpdateHolidayAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid calendarId,
            [FromRoute] DateOnly date,
            [FromBody] UpdateHolidayRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new UpdateHolidayCommand(
                calendarId,
                branchId,
                date,
                request.NewName,
                request.NewType,
                request.IsOptional);

            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleRemoveHolidayAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid calendarId,
            [FromRoute] DateOnly date,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new RemoveHolidayCommand(calendarId, branchId, date);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        private static async Task<IResult> HandleClearHolidaysAsync(
            [FromRoute] Guid branchId,
            [FromRoute] Guid calendarId,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken)
        {
            var command = new ClearHolidaysCommand(calendarId, branchId);
            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        }

        #endregion
    }
}
