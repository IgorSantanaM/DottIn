namespace DottIn.Infra.Services.Auth
{
    public interface ITokenService
    {
        string GenerateToken(Guid employeeId, Guid branchId, string secretKey, string issuer, string audience, int expirationMinutes);
    }
}
