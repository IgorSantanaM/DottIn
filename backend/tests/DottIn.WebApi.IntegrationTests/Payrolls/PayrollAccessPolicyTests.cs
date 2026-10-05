using DottIn.Domain.Employees;
using DottIn.Presentation.WebApi.Security;

namespace DottIn.WebApi.IntegrationTests.Payrolls;

public sealed class PayrollAccessPolicyTests
{
    [Fact]
    public void OnlyOwnerOrAdministratorOfTheBranchTenantCanSetPayments()
    {
        var tenant = Guid.NewGuid();
        var other = Guid.NewGuid();
        Assert.True(PayrollAccessPolicy.CanManage(EmployeeRole.Owner, tenant, tenant));
        Assert.True(PayrollAccessPolicy.CanManage(EmployeeRole.Administrator, tenant, tenant));
        Assert.False(PayrollAccessPolicy.CanManage(EmployeeRole.Owner, tenant, other));
        Assert.False(PayrollAccessPolicy.CanManage(EmployeeRole.Accountant, tenant, tenant));
        Assert.False(PayrollAccessPolicy.CanManage(EmployeeRole.Manager, tenant, tenant));
    }

    [Fact]
    public void AccountantNeedsExplicitGrantEvenForHomeTenant()
    {
        var tenant = Guid.NewGuid();
        var otherCompany = Guid.NewGuid();
        Assert.False(PayrollAccessPolicy.CanView(EmployeeRole.Accountant, tenant, tenant, false));
        Assert.True(PayrollAccessPolicy.CanView(EmployeeRole.Accountant, tenant, otherCompany, true));
        Assert.True(PayrollAccessPolicy.CanExport(EmployeeRole.Accountant, otherCompany, true));
        Assert.False(PayrollAccessPolicy.CanExport(EmployeeRole.Accountant, otherCompany, false));
        Assert.False(PayrollAccessPolicy.CanExport(EmployeeRole.Owner, tenant, true));
        Assert.False(PayrollAccessPolicy.CanView(EmployeeRole.Accountant, tenant, null, true));
    }
}
