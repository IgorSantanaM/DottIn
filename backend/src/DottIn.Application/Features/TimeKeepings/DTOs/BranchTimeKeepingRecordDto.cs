namespace DottIn.Application.Features.TimeKeepings.DTOs;

public record BranchTimeKeepingRecordDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateOnly WorkDate,
    DateTime? ClockIn,
    DateTime? ClockOut,
    TimeSpan TotalWorked,
    TimeSpan TotalBreak,
    string Status,
    bool IsNocturnal,
    string Source,
    bool IsHoliday = false,
    string? HolidayName = null);
