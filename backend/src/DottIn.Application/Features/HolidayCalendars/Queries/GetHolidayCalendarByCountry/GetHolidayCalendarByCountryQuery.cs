using DottIn.Application.Features.HolidayCalendars.DTOs;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Queries.GetHolidayCalendarByCountry;

public record GetHolidayCalendarByCountryQuery(Guid BranchId, string CountryCode, int Year, string? RegionCode) : IRequest<HolidayCalendarSummaryDto>;
