using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

public class AdminState
{
    public bool IsAuthenticated { get; private set; }
    public string AccessToken { get; private set; } = "";
    public EmployeeInfo? User { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid BranchId { get; private set; }
    public bool IsOwner { get; private set; }
    public bool IsDarkMode { get; private set; }
    public string CompanyCode { get; private set; } = "";

    public event Action? OnChange;

    public void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        OnChange?.Invoke();
    }

    public void SetAuthenticated(LoginResponse response, string companyCode)
    {
        IsAuthenticated = true;
        AccessToken = response.AccessToken;
        User = response.Employee;
        EmployeeId = response.Employee.Id;
        BranchId = response.BranchId;
        IsOwner = response.IsOwner;
        CompanyCode = companyCode;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        IsAuthenticated = false;
        AccessToken = "";
        User = null;
        EmployeeId = Guid.Empty;
        BranchId = Guid.Empty;
        IsOwner = false;
        CompanyCode = "";
        OnChange?.Invoke();
    }
}
