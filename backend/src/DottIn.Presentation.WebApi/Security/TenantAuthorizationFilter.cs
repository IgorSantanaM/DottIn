using System.Reflection;

namespace DottIn.Presentation.WebApi.Security;

public sealed class TenantAuthorizationFilter(TenantAccessService access, CurrentUserContext currentUser)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!currentUser.IsAuthenticated)
            return Results.Unauthorized();

        var http = context.HttpContext;
        var mutation = HttpMethods.IsPost(http.Request.Method) || HttpMethods.IsPut(http.Request.Method) ||
                       HttpMethods.IsPatch(http.Request.Method) || HttpMethods.IsDelete(http.Request.Method);
        var path = http.Request.Path.Value ?? "";

        var branchId = ReadRouteGuid(http, "branchId") ?? ReadArgumentGuid(context.Arguments, "BranchId");
        var employeeId = ReadRouteGuid(http, "employeeId") ?? ReadArgumentGuid(context.Arguments, "EmployeeId");
        var timeKeepingId = ReadRouteGuid(http, "timeKeepingId");
        var ownerId = ReadRouteGuid(http, "ownerId");

        if (path.Contains("/timekeeping/employee/", StringComparison.OrdinalIgnoreCase) &&
            path.EndsWith("/history/paged", StringComparison.OrdinalIgnoreCase) &&
            employeeId != currentUser.EmployeeId && !currentUser.IsAdministrator)
            return Results.Forbid();

        if (ownerId.HasValue && ownerId.Value != currentUser.TenantId)
            return Results.Forbid();

        if (timeKeepingId.HasValue && !await access.CanAccessTimeKeepingAsync(timeKeepingId.Value, http.RequestAborted))
            return Results.Forbid();

        var isClockAction = path.Contains("/timekeeping/clock-", StringComparison.OrdinalIgnoreCase) ||
                            path.EndsWith("/timekeeping/break", StringComparison.OrdinalIgnoreCase);

        if (isClockAction && employeeId.HasValue)
        {
            var skip = ReadArgumentBool(context.Arguments, "SkipGeolocationValidation");
            if (!access.CanActFor(employeeId.Value, skip))
                return Results.Forbid();
            if (employeeId.Value != currentUser.EmployeeId &&
                !await access.CanAccessEmployeeAsync(employeeId.Value, mutation: false, http.RequestAborted))
                return Results.Forbid();
        }
        else if (employeeId.HasValue && !await access.CanAccessEmployeeAsync(employeeId.Value, mutation, http.RequestAborted))
        {
            return Results.Forbid();
        }

        if (branchId.HasValue)
        {
            var requireAdmin = mutation && IsBranchAdministrationPath(path) ||
                               path.Contains("/timekeeping/branch/", StringComparison.OrdinalIgnoreCase);
            var requireManager = RequiresManager(path, mutation, isClockAction);
            if (!await access.CanAccessBranchAsync(branchId.Value, requireManager, requireAdmin, http.RequestAborted))
                return Results.Forbid();
        }

        return await next(context);
    }

    private static bool IsBranchAdministrationPath(string path)
        => path.StartsWith("/api/branches", StringComparison.OrdinalIgnoreCase) &&
           !path.Contains("/employees", StringComparison.OrdinalIgnoreCase) &&
           !path.Contains("/holiday-calendars", StringComparison.OrdinalIgnoreCase) &&
           !path.Contains("/dominio-mappings", StringComparison.OrdinalIgnoreCase) &&
           !path.Contains("/exports/", StringComparison.OrdinalIgnoreCase);

    private static bool RequiresManager(string path, bool mutation, bool isClockAction)
    {
        if (mutation)
            return !isClockAction;

        return path.Contains("/employee-invitations", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/timekeeping-adjustments", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/dominio-mappings", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/exports/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/timekeeping/branch/", StringComparison.OrdinalIgnoreCase) ||
               path.Contains("/holiday-calendars/work-records/", StringComparison.OrdinalIgnoreCase) ||
               IsEmployeeDirectoryPath(path);
    }

    private static bool IsEmployeeDirectoryPath(string path)
    {
        var marker = "/employees";
        var index = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return false;

        var suffix = path[(index + marker.Length)..].TrimEnd('/');
        return suffix.Length == 0 ||
               suffix.Equals("/active", StringComparison.OrdinalIgnoreCase) ||
               suffix.StartsWith("/cpf/", StringComparison.OrdinalIgnoreCase);
    }

    private static Guid? ReadRouteGuid(HttpContext context, string key)
        => Guid.TryParse(context.Request.RouteValues[key]?.ToString(), out var id) ? id : null;

    private static Guid? ReadArgumentGuid(IList<object?> arguments, string property)
    {
        foreach (var argument in arguments.Where(x => x is not null))
        {
            var info = argument!.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (info?.GetValue(argument) is Guid id && id != Guid.Empty)
                return id;
        }
        return null;
    }

    private static bool ReadArgumentBool(IList<object?> arguments, string property)
    {
        foreach (var argument in arguments.Where(x => x is not null))
        {
            var info = argument!.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (info?.GetValue(argument) is bool value)
                return value;
        }
        return false;
    }
}
