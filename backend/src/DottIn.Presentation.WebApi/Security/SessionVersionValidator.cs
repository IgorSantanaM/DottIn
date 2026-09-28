using System.Security.Claims;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Security;

internal static class SessionVersionValidator
{
    public static async Task<bool> IsValidAsync(
        ClaimsPrincipal? principal,
        DottInContext db,
        CancellationToken cancellationToken)
    {
        var employeeClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? principal?.FindFirstValue("sub");
        var versionClaim = principal?.FindFirstValue("sessionVersion");
        if (!Guid.TryParse(employeeClaim, out var employeeId)
            || !Guid.TryParse(versionClaim, out var sessionVersion))
            return false;

        return await db.Employees.AsNoTracking().AnyAsync(
            employee => employee.Id == employeeId
                        && employee.IsActive
                        && employee.SessionVersion == sessionVersion,
            cancellationToken);
    }
}
