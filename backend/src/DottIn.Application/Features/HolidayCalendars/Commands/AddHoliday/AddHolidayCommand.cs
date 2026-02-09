using DottIn.Domain.HolidayCalendars;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.AddHoliday;

public record AddHolidayCommand(Guid HolidayCalendarId,
        Guid BranchId,
        IEnumerable<(DateOnly Date, string Name, HolidayType Type, bool IsOptional)> Holidays) : IRequest<Unit>;

