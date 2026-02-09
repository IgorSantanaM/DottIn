namespace DottIn.Application.Features.HolidayCalendars.DTOs;

public record HolidayCalendarDetailsDto(string BranchName,
        string Name,
        string? Description,
        string CountryCode,
        string? RegionCode,
        int Year,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? UpdatedAt);
