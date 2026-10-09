using DottIn.Domain.Employees;
using DottIn.Infra.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace DottIn.Presentation.WebApi.Security;

public static class CompanyJoinMembership
{
    // Resolve ownership from current database records, not cached branch/JWT claims.
    // Invites to another branch of the same tenant must never transfer the employee.
    public static Task<bool> IsMemberAsync(
        DottInContext db, Guid employeeId, Guid ownerId, CancellationToken cancellationToken)
        => db.Employees.AnyAsync(employee => employee.Id == employeeId && employee.IsActive &&
            ((employee.Role == EmployeeRole.Owner && employee.Id == ownerId) ||
             db.Branches.Any(branch => branch.Id == employee.BranchId && branch.IsActive &&
                                       branch.OwnerId == ownerId)), cancellationToken);
}
