namespace DottIn.Infra.Services.Auth
{
    public interface ITokenService
    {
        string GenerateToken(
            Guid employeeId,
            Guid branchId,
            Guid tenantId,
            string role,
            string secretKey,
            string issuer,
            string audience,
            int expirationMinutes);
    }
}
