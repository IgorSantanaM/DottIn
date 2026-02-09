using DottIn.Application.Features.HolidayCalendars.DTOs;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidayCalendarById;

public record GetHolidayCalendarByIdQuery(Guid HolidayCalendarId, Guid BranchId) : IRequest<HolidayCalendarDetailsDto>;
