namespace DottIn.Presentation.WebApi.DTOs.Auth
{
    public record LoginRequest(string Cpf, string Password, string CompanyCode);
    public record PinLoginRequest(string Cpf, string Pin, string CompanyCode);
    public record RegisterFingerprintRequest(string CompanyCode, string Cpf, string Password, string FingerprintToken);
    public record FingerprintLoginRequest(string CompanyCode, string Cpf, string FingerprintToken);

    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt,
        EmployeeInfoDto Employee,
        Guid BranchId);

    public record EmployeeInfoDto(
        Guid Id,
        string Name,
        string Cpf,
        string? ImageUrl);
}
