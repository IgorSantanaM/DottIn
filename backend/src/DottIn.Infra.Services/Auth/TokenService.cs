using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DottIn.Infra.Services.Auth
{
    public class TokenService : ITokenService
    {
        public string GenerateToken(
            Guid employeeId,
            Guid branchId,
            Guid tenantId,
            string role,
            string secretKey,
            string issuer,
            string audience,
            int expirationMinutes)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, employeeId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, employeeId.ToString()),
                new Claim("branchId", branchId.ToString()),
                new Claim("tenantId", tenantId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
