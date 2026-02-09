namespace DottIn.Presentation.WebApi.DTOs.HolidayCalendars
{
    public record CreateHolidayCalendarRequest(
         string Name,
         string CountryCode,
         int Year,
         string? RegionCode,
         string? Description);
}
