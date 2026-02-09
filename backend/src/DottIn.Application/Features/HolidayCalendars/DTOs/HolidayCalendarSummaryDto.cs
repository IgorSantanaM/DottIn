namespace DottIn.Application.Features.HolidayCalendars.DTOs;

public record HolidayCalendarSummaryDto(string BranchName,
            string Name,
            string? Description,
            string CountryCode,
            string? RegionCode,
            int Year,
            bool IsActive);