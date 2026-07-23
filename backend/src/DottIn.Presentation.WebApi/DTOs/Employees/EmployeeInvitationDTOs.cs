using DottIn.Domain.Employees;

namespace DottIn.Presentation.WebApi.DTOs.Employees;

public sealed record CreateEmployeeInvitationRequest(
    string? Email,
    EmployeeRole Role = EmployeeRole.Employee,
    int ExpiresInHours = 72);

public sealed record EmployeeInvitationCreatedResponse(
    Guid InvitationId,
    string Token,
    DateTime ExpiresAt);

public sealed record EmployeeInvitationResponse(
    Guid Id,
    Guid BranchId,
    string? Email,
    EmployeeRole Role,
    InvitationStatus Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? ConsumedAt,
    DateTime? RevokedAt);

public sealed record AcceptEmployeeInvitationRequest(
    string Token,
    string Name,
    string Cpf,
    string Password,
    TimeOnly StartWorkTime,
    TimeOnly EndWorkTime,
    TimeOnly IntervalStart,
    TimeOnly IntervalEnd);

public sealed record AcceptEmployeeInvitationResponse(Guid EmployeeId, Guid BranchId);
