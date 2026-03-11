namespace DottIn.Application.Features.TimeKeepings.DTOs;

public record TimeKeepingRecordDto(
    Guid Id,
    DateOnly WorkDate,
    DateTime? ClockIn,
    DateTime? ClockOut,
    TimeSpan TotalWorked,
    TimeSpan TotalBreak,
    string Status,
    bool IsNocturnal);
