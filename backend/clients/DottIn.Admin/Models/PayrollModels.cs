namespace DottIn.Admin.Models;

public record PayrollSummary(Guid Id, Guid BranchId, int Year, int Month,
    string Status, string BranchName, int EmployeeCount, decimal TotalAmount);
public record PayrollLine(Guid EmployeeId, string EmployeeName, string? DominioCode,
    long WorkedMinutes, bool HasIncompleteRecords, decimal? CalculatedAmount,
    decimal? PaymentAmount, string? Notes);
public record PayrollDetails(Guid Id, Guid BranchId, int Year, int Month,
    string Status, Guid Version, DateTime? ClosedAt,
    IReadOnlyList<PayrollLine> Items, decimal TotalAmount);
public record AccountantInvitation(Guid InvitationId, string Token, DateTime ExpiresAt);
public record AccountantAccess(Guid EmployeeId, string Name, DateTime GrantedAt);
public record AccountantAccessRequest(string Token);
public record AccountantRegisterRequest(string Token, string Name, string Cpf, string Password,
    TimeOnly StartWorkTime, TimeOnly EndWorkTime, TimeOnly IntervalStart, TimeOnly IntervalEnd);
