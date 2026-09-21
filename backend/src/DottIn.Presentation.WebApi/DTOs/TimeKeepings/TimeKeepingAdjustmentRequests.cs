using DottIn.Domain.TimeKeepings;

namespace DottIn.Presentation.WebApi.DTOs.TimeKeepings;

public sealed record CreateTimeKeepingAdjustmentRequest(
    TimeKeepingType EntryType,
    DateTime? OriginalTimestamp,
    DateTime ProposedTimestamp,
    string Reason);

public sealed record ReviewTimeKeepingAdjustmentRequest(
    bool Approve,
    string? ReviewNote);

public sealed record TimeKeepingAdjustmentResponse(
    Guid Id,
    Guid TimeKeepingId,
    Guid EmployeeId,
    string EmployeeName,
    Guid RequestedByEmployeeId,
    string RequestedByName,
    Guid? ReviewedByEmployeeId,
    string? ReviewedByName,
    TimeKeepingType EntryType,
    DateTime? OriginalTimestamp,
    DateTime ProposedTimestamp,
    string Reason,
    string? ReviewNote,
    TimeKeepingAdjustmentStatus Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
