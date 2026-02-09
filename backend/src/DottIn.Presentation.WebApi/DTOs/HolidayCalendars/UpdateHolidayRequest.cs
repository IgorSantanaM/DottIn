using DottIn.Domain.HolidayCalendars;

namespace DottIn.Presentation.WebApi.DTOs.HolidayCalendars;

public record UpdateHolidayRequest(
     string? NewName,
     HolidayType? NewType,
     bool? IsOptional);
