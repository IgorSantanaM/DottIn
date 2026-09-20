namespace DottIn.Mobile.Services;

public static class MobileRoutePolicy
{
    public static bool IsPublic(string path) => path is "/" or "/login" or "/register";
    public static bool IsManagement(string path) => path == "/management" || path.StartsWith("/management/", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/onboarding/", StringComparison.OrdinalIgnoreCase);
    public static string? Redirect(string path, bool authenticated, bool owner, Guid branchId)
    {
        if (IsPublic(path)) return null;
        if (!authenticated) return "/login";
        if (IsManagement(path) && !owner) return "/dashboard";
        if (owner && branchId == Guid.Empty && path != "/onboarding/company") return "/onboarding/company";
        return null;
    }
}
