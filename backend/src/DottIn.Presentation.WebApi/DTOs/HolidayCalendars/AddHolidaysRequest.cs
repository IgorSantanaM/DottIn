using DottIn.Presentation.WebApi.Endpoints;

namespace DottIn.Presentation.WebApi.DTOs.HolidayCalendars
{
    public record AddHolidaysRequest(IEnumerable<HolidayRequest> Holidays);
}
