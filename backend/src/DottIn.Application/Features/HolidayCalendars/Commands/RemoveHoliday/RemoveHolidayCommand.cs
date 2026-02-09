using MediatR;

namespace DottIn.Application.Features.HolidayCalendars.Commands.RemoveHoliday
{
    public record RemoveHolidayCommand(Guid HolidayCalendarId, Guid BranchId, DateOnly Date) : IRequest<Unit>;
}
