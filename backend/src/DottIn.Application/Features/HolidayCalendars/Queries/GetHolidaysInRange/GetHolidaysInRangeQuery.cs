using DottIn.Application.Features.HolidayCalendars.DTOs;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidaysInRange;

public record GetHolidaysInRangeQuery(Guid BranchId, DateOnly StartDate, DateOnly EndDate) : IRequest<IEnumerable<HolidayDto>>;
