namespace DottIn.Presentation.WebApi.DTOs.Employees;

public sealed record CompanyJoinLinkResponse(string Token, DateTime ExpiresAt, string CompanyName);
public sealed record CompanyJoinLinkResolutionResponse(string CompanyName, bool CanJoin);
public sealed record RegisterFromCompanyJoinLinkRequest(string Token, string Name, string Cpf, string Password);
public sealed record RegisterFromCompanyJoinLinkResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid EmployeeId,
    Guid BranchId);
