using System.Security.Claims;
using DottIn.Domain.Employees;

namespace DottIn.Presentation.WebApi.Security;

public sealed class CurrentUserContext(IHttpContextAccessor accessor)
{
    private ClaimsPrincipal User => accessor.HttpContext?.User
        ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public Guid EmployeeId => ReadGuid(ClaimTypes.NameIdentifier, "sub");
    public Guid BranchId => ReadOptionalGuid("branchId");
    public Guid TenantId => ReadGuid("tenantId");

    public EmployeeRole Role
        => Enum.TryParse<EmployeeRole>(User.FindFirstValue(ClaimTypes.Role), true, out var role)
            ? role
            : EmployeeRole.Employee;

    public bool IsOwner => Role == EmployeeRole.Owner;
    public bool IsAdministrator => Role is EmployeeRole.Owner or EmployeeRole.Administrator;
    public bool IsManager => Role is EmployeeRole.Owner or EmployeeRole.Administrator or EmployeeRole.Manager;

    private Guid ReadGuid(params string[] claimTypes)
    {
        var value = claimTypes.Select(User.FindFirstValue).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            throw new UnauthorizedAccessException("Token de acesso inválido.");
        return id;
    }

    private Guid ReadOptionalGuid(string claimType)
        => Guid.TryParse(User.FindFirstValue(claimType), out var id) ? id : Guid.Empty;
}
