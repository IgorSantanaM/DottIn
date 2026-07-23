namespace DottIn.Admin.Services;

public static class OperationalAccessPolicy
{
    public static bool CanAccessModules(
        Guid branchId,
        bool hasLinkedPlan,
        bool isResolved)
        => isResolved && branchId != Guid.Empty && hasLinkedPlan;

    public static string GetAuthenticatedDestination(bool canAccessModules)
        => canAccessModules ? "/dashboard" : "/welcome";
}
