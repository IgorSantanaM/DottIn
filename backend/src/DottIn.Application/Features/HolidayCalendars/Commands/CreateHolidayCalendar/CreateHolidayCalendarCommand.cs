using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.AddHolidays;

public record CreateHolidayCalendarCommand(Guid BranchId, 
                string Name,
                string CountryCode, 
                int Year, 
                string? RegionCode, 
                string? Description) : IRequest<Guid>;
