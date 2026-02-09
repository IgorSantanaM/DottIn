using DottIn.Domain.HolidayCalendars;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.CreateHolidayCalendar;

public record AddHolidayCommand(Guid HolidayCalendarId,
        Guid BranchId,
        DateOnly Date,
        string Name,
        HolidayType Type,
        bool IsOptional) : IRequest<Unit>;

