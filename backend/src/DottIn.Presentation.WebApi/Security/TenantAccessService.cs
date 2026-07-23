using DottIn.Domain.Employees;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Security;

public sealed class TenantAccessService(DottInContext db, CurrentUserContext currentUser)
{
    public async Task<bool> CanAccessBranchAsync(Guid branchId, bool requireManager = false, bool requireAdministrator = false, CancellationToken token = default)
    {
        if (!currentUser.IsAuthenticated || branchId == Guid.Empty)
            return false;
        if (requireAdministrator && !currentUser.IsAdministrator)
            return false;
        if (requireManager && !currentUser.IsManager)
            return false;

        var branch = await db.Branches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == branchId, token);
        if (branch?.OwnerId != currentUser.TenantId)
            return false;

        return currentUser.Role is EmployeeRole.Owner or EmployeeRole.Administrator ||
               currentUser.BranchId == branchId;
    }

    public async Task<bool> CanAccessEmployeeAsync(Guid employeeId, bool mutation, CancellationToken token = default)
    {
        if (!currentUser.IsAuthenticated || employeeId == Guid.Empty)
            return false;
        if (employeeId == currentUser.EmployeeId)
            return !mutation;
        if (!currentUser.IsManager)
            return false;

        var branchId = await db.Employees.AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => x.BranchId)
            .FirstOrDefaultAsync(token);

        return await CanAccessBranchAsync(branchId, requireManager: true, token: token);
    }

    public async Task<bool> CanAccessTimeKeepingAsync(Guid timeKeepingId, CancellationToken token = default)
    {
        var data = await db.TimeKeepings.AsNoTracking()
            .Where(x => x.Id == timeKeepingId)
            .Select(x => new { x.BranchId, x.EmployeeId })
            .FirstOrDefaultAsync(token);

        if (data is null) return false;
        if (data.EmployeeId == currentUser.EmployeeId) return true;
        return await CanAccessBranchAsync(data.BranchId, requireManager: true, token: token);
    }

    public bool CanActFor(Guid employeeId, bool requestedSkipGeolocation)
        => employeeId == currentUser.EmployeeId
            ? !requestedSkipGeolocation || currentUser.IsAdministrator
            : currentUser.IsManager;
}
