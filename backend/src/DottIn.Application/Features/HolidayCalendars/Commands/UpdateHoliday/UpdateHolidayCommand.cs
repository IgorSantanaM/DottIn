using DottIn.Domain.HolidayCalendars;
using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.UpdateHoliday;

public record UpdateHolidayCommand(Guid HolidayCalendarId,
    Guid BranchId,
    DateOnly NewDate,
    string? NewName,
    HolidayType? NewType,
    bool? IsOptional)
    : IRequest<Unit>;
