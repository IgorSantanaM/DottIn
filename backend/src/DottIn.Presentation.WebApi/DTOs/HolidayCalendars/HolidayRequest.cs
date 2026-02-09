using DottIn.Domain.HolidayCalendars;

namespace DottIn.Presentation.WebApi.DTOs.HolidayCalendars
{
    public record HolidayRequest(
        DateOnly Date,
        string Name,
        HolidayType Type,
        bool IsOptional);
}
