using DottIn.Domain.HolidayCalendars;

namespace DottIn.Application.Features.HolidayCalendars.DTOs;

public record HolidayDto(DateOnly Date,
    string Name,
    HolidayType Type,
    bool IsOptional);
