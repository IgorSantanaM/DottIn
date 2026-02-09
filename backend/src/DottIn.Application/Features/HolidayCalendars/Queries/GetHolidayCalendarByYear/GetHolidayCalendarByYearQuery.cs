using DottIn.Application.Features.Branches.DTOs;
using DottIn.Application.Features.HolidayCalendars.DTOs;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidayCalendarByYear;

public record GetHolidayCalendarByYearQuery(Guid BranchId, int Year) : IRequest<HolidayCalendarSummaryDto>;
