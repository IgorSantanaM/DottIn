namespace DottIn.Presentation.WebApi.DTOs.Auth
{
    public record LoginRequest(string Cpf, string Password, string CompanyCode);
    public record PinLoginRequest(string Cpf, string Pin, string CompanyCode);
    public record RegisterFingerprintRequest(string CompanyCode, string Cpf, string Password, string FingerprintToken);
    public record FingerprintLoginRequest(string CompanyCode, string Cpf, string FingerprintToken);
    public record ChangePasswordRequest(string CompanyCode, string Cpf, string CurrentPassword, string NewPassword);
    public record ChangePinRequest(string CompanyCode, string Cpf, string CurrentPassword, string NewPin);
    public record RefreshTokenRequest(string RefreshToken);

    public record LoginResponse(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt,
        EmployeeInfoDto Employee,
        Guid BranchId,
        bool IsOwner,
        bool IsHeadquarters);

    public record RefreshTokenResponse(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt);

    public record EmployeeInfoDto(
        Guid Id,
        string Name,
        string Cpf,
        string? ImageUrl);
}
