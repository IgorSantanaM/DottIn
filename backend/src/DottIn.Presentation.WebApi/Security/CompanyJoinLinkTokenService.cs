using System.Globalization;
using DottIn.Domain.Employees;
using Microsoft.AspNetCore.DataProtection;

namespace DottIn.Presentation.WebApi.Security;

public interface ICompanyJoinLinkTokenService
{
    string CreateToken(CompanyJoinLink link);
    bool TryReadToken(string token, out Guid linkId, out DateTime expiresAtUtc);
}

public sealed class CompanyJoinLinkTokenService(IDataProtectionProvider dataProtectionProvider)
    : ICompanyJoinLinkTokenService
{
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("DottIn.CompanyJoinLink.v1");

    public string CreateToken(CompanyJoinLink link) => _protector.Protect(
        $"{link.Id:N}|{link.ExpiresAt.Ticks.ToString(CultureInfo.InvariantCulture)}");

    public bool TryReadToken(string token, out Guid linkId, out DateTime expiresAtUtc)
    {
        linkId = Guid.Empty;
        expiresAtUtc = default;
        try
        {
            var parts = _protector.Unprotect(token).Split('|');
            if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "N", out linkId) ||
                !long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                return false;

            expiresAtUtc = new DateTime(ticks, DateTimeKind.Utc);
            return expiresAtUtc > DateTime.UtcNow;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
