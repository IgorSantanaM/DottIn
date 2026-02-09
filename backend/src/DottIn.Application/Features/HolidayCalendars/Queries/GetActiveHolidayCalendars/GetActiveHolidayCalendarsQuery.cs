using DottIn.Application.Features.HolidayCalendars.DTOs;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetActiveHolidayCalendars;

public record GetActiveHolidayCalendarsQuery(Guid BranchId) : IRequest<IEnumerable<HolidayCalendarSummaryDto>>;
