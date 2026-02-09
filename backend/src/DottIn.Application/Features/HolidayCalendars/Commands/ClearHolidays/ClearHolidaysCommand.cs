using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.ClearHolidays;

public record ClearHolidaysCommand(Guid HolidayCalendarId, Guid BranchId) : IRequest<Unit>;
