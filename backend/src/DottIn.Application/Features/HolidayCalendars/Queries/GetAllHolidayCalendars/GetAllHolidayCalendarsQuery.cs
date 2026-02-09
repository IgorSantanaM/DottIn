using DottIn.Application.Features.HolidayCalendars.DTOs;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetAllHolidayCalendars;

public record GetAllHolidayCalendarsQuery(Guid BranchId) : IRequest<IEnumerable<HolidayCalendarSummaryDto>>;
