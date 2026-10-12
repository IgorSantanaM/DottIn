namespace DottIn.Mobile.Services;

public class AppState
{
    public bool IsAuthenticated { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid BranchId { get; private set; }
    public bool IsOwner { get; private set; }
    public bool IsHeadquarters { get; private set; }
    public EmployeeInfo? Employee { get; private set; }
    public List<BranchOption> AvailableBranches { get; private set; } = [];
    public string SelectedBranchName { get; private set; } = "";
    public bool IsDarkMode { get; private set; }
    public string ThemePreference { get; private set; } = "system";
    public string Role { get; private set; } = "Employee";
    public bool CanEditUnitSettings => IsAuthenticated && Role is "Owner" or "Administrator";

    public bool CanSwitchBranches => IsOwner && IsHeadquarters && AvailableBranches.Count > 1;

    public event Action? OnChange;

    public void SetAuthenticated(EmployeeInfo employee, Guid branchId, bool isOwner = false, bool isHeadquarters = false, string? role = null)
    {
        IsAuthenticated = true;
        Employee = employee;
        EmployeeId = employee.Id;
        BranchId = branchId;
        Role = role ?? (isOwner ? "Owner" : "Employee");
        IsOwner = Role == "Owner";
        IsHeadquarters = isHeadquarters;
        SelectedBranchName = AvailableBranches.FirstOrDefault(b => b.Id == branchId)?.Name ?? "";
        NotifyStateChanged();
    }

    public void SetAvailableBranches(IEnumerable<BranchOption> branches)
    {
        AvailableBranches = branches.ToList();
        var current = AvailableBranches.FirstOrDefault(b => b.Id == BranchId);
        SelectedBranchName = current?.Name ?? "";
        NotifyStateChanged();
    }

    public void SwitchBranch(Guid branchId)
    {
        var branch = AvailableBranches.FirstOrDefault(b => b.Id == branchId);
        if (branch is null) return;
        BranchId = branchId;
        SelectedBranchName = branch.Name;
        NotifyStateChanged();
    }

    public void SetDarkMode(bool isDarkMode)
    {
        IsDarkMode = isDarkMode;
        NotifyStateChanged();
    }

    public void SetTheme(string preference, bool systemDark)
    {
        ThemePreference = preference is "light" or "dark" or "system" ? preference : "system";
        SetDarkMode(ThemePreference == "dark" || ThemePreference == "system" && systemDark);
    }

    public void Logout()
    {
        IsAuthenticated = false;
        Employee = null;
        EmployeeId = Guid.Empty;
        BranchId = Guid.Empty;
        IsOwner = false;
        Role = "Employee";
        IsHeadquarters = false;
        AvailableBranches = [];
        SelectedBranchName = "";
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

public record BranchOption(Guid Id, string Name, bool IsHeadquarters);

public record EmployeeInfo(
    Guid Id,
    string Name,
    string Cpf,
    string? ImageUrl)
{
    public string FirstName => Name.Split(' ').FirstOrDefault() ?? Name;
    public string Initials => string.Join("", Name.Split(' ').Take(2).Select(n => n.FirstOrDefault()));
}
