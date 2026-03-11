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

    public bool CanSwitchBranches => IsOwner && IsHeadquarters && AvailableBranches.Count > 1;

    public event Action? OnChange;

    public void SetAuthenticated(EmployeeInfo employee, Guid branchId, bool isOwner = false, bool isHeadquarters = false)
    {
        IsAuthenticated = true;
        Employee = employee;
        EmployeeId = employee.Id;
        BranchId = branchId;
        IsOwner = isOwner;
        IsHeadquarters = isHeadquarters;
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

    public void Logout()
    {
        IsAuthenticated = false;
        Employee = null;
        EmployeeId = Guid.Empty;
        BranchId = Guid.Empty;
        IsOwner = false;
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
