using DottIn.Domain.Employees;

namespace DottIn.Presentation.WebApi.Security;

public static class PayrollAccessPolicy
{
    public static bool CanManage(EmployeeRole role, Guid tenantId, Guid? branchOwnerId)
        => role is EmployeeRole.Owner or EmployeeRole.Administrator &&
           branchOwnerId.HasValue && branchOwnerId.Value == tenantId;

    public static bool CanView(EmployeeRole role, Guid tenantId, Guid? branchOwnerId, bool explicitGrant)
        => CanManage(role, tenantId, branchOwnerId) ||
           role == EmployeeRole.Accountant && branchOwnerId.HasValue && explicitGrant;

    public static bool CanExport(EmployeeRole role, Guid? branchOwnerId, bool explicitGrant)
        => role == EmployeeRole.Accountant && branchOwnerId.HasValue && explicitGrant;
}
