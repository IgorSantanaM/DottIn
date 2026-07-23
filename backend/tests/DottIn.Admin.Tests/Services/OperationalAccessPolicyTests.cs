using DottIn.Admin.Services;
using DottIn.Admin.Routing;

namespace DottIn.Admin.Tests.Services;

public sealed class OperationalAccessPolicyTests
{
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public void CanAccessModules_RequiresConfigurationPlanAndResolvedStatus(
        bool hasConfiguration,
        bool hasLinkedPlan,
        bool isResolved,
        bool expected)
    {
        var branchId = hasConfiguration ? Guid.NewGuid() : Guid.Empty;

        var result = OperationalAccessPolicy.CanAccessModules(
            branchId,
            hasLinkedPlan,
            isResolved);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(false, "/welcome")]
    [InlineData(true, "/dashboard")]
    public void GetAuthenticatedDestination_UsesOperationalEligibility(
        bool canAccessModules,
        string expected)
    {
        Assert.Equal(
            expected,
            OperationalAccessPolicy.GetAuthenticatedDestination(canAccessModules));
    }

    [Theory]
    [MemberData(nameof(OperationalPages))]
    public void OperationalPages_RequireOperationalAccess(Type pageType)
    {
        Assert.True(Attribute.IsDefined(pageType, typeof(OperationalAccessRequiredAttribute)));
    }

    public static TheoryData<Type> OperationalPages => new()
    {
        typeof(DottIn.Admin.Pages.Dashboard),
        typeof(DottIn.Admin.Pages.Employees),
        typeof(DottIn.Admin.Pages.TimeKeeping),
        typeof(DottIn.Admin.Pages.Holidays)
    };
}
