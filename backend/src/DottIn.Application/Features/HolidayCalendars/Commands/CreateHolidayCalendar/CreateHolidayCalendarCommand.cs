using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.CreateHolidayCalendar;

public record CreateHolidayCalendarCommand(Guid BranchId,
                string Name,
                string CountryCode,
                int Year,
                string? RegionCode,
                string? Description) : IRequest<Guid>;
