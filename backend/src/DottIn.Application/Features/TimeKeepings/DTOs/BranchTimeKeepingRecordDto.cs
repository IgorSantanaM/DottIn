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
    string Status);
