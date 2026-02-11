namespace DottIn.Mobile.Services;

public class AppState
{
    public bool IsAuthenticated { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid BranchId { get; private set; }
    public EmployeeInfo? Employee { get; private set; }

    public event Action? OnChange;

    public void SetAuthenticated(EmployeeInfo employee, Guid branchId)
    {
        IsAuthenticated = true;
        Employee = employee;
        EmployeeId = employee.Id;
        BranchId = branchId;
        NotifyStateChanged();
    }

    public void Logout()
    {
        IsAuthenticated = false;
        Employee = null;
        EmployeeId = Guid.Empty;
        BranchId = Guid.Empty;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

public record EmployeeInfo(
    Guid Id,
    string Name,
    string Cpf,
    string? ImageUrl)
{
    public string FirstName => Name.Split(' ').FirstOrDefault() ?? Name;
    public string Initials => string.Join("", Name.Split(' ').Take(2).Select(n => n.FirstOrDefault()));
}
