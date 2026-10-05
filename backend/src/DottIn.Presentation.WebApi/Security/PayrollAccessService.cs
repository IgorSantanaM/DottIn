using DottIn.Domain.Employees;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Security;

public sealed class PayrollAccessService(DottInContext db, CurrentUserContext currentUser)
{
    public async Task<bool> CanManageAsync(Guid branchId, CancellationToken token)
    {
        if (!currentUser.IsAuthenticated) return false;
        var ownerId = await db.Branches.AsNoTracking().Where(b => b.Id == branchId)
            .Select(b => b.OwnerId).FirstOrDefaultAsync(token);
        return PayrollAccessPolicy.CanManage(currentUser.Role, currentUser.TenantId, ownerId);
    }

    public async Task<bool> CanViewAsync(Guid branchId, CancellationToken token)
    {
        if (!currentUser.IsAuthenticated) return false;
        var ownerId = await db.Branches.AsNoTracking().Where(b => b.Id == branchId)
            .Select(b => b.OwnerId).FirstOrDefaultAsync(token);
        if (PayrollAccessPolicy.CanManage(currentUser.Role, currentUser.TenantId, ownerId)) return true;
        var explicitGrant = currentUser.Role == EmployeeRole.Accountant &&
            await db.AccountantBranchAccesses.AsNoTracking().AnyAsync(a =>
                a.BranchId == branchId && a.AccountantEmployeeId == currentUser.EmployeeId, token);
        return PayrollAccessPolicy.CanView(currentUser.Role, currentUser.TenantId, ownerId, explicitGrant);
    }

    public async Task<bool> CanExportAsync(Guid branchId, CancellationToken token)
    {
        if (currentUser.Role != EmployeeRole.Accountant) return false;
        return await CanViewAsync(branchId, token);
    }

    public async Task<Guid[]> VisibleBranchIdsAsync(CancellationToken token)
    {
        if (currentUser.IsAdministrator)
            return await db.Branches.AsNoTracking()
                .Where(b => b.OwnerId == currentUser.TenantId)
                .Select(b => b.Id).ToArrayAsync(token);
        if (currentUser.Role == EmployeeRole.Accountant)
            return await db.AccountantBranchAccesses.AsNoTracking()
                .Where(a => a.AccountantEmployeeId == currentUser.EmployeeId &&
                    db.Branches.Any(b => b.Id == a.BranchId && b.OwnerId != null))
                .Select(a => a.BranchId).ToArrayAsync(token);
        return [];
    }
}
