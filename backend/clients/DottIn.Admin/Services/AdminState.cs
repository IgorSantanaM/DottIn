using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

public class AdminState(SessionStorageService storage)
{
    private const string SessionKey = "admin.session";

    public bool IsAuthenticated { get; private set; }
    public string AccessToken { get; private set; } = "";
    public string RefreshToken { get; private set; } = "";
    public DateTime? ExpiresAt { get; private set; }
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

    public async Task SetAuthenticatedAsync(LoginResponse response, string companyCode)
    {
        var session = new AdminSession(
            response.AccessToken,
            response.RefreshToken,
            response.ExpiresAt,
            response.Employee,
            response.BranchId,
            response.IsOwner,
            companyCode);

        ApplySession(session);
        await storage.SetItemAsync(SessionKey, session);
        OnChange?.Invoke();
    }

    public async Task SetAuthenticatedAsync(AdminSession session)
    {
        ApplySession(session);
        await storage.SetItemAsync(SessionKey, session);
        OnChange?.Invoke();
    }

    public Task<AdminSession?> GetSessionAsync()
        => storage.GetItemAsync<AdminSession>(SessionKey);

    public async Task LogoutAsync()
    {
        ResetState();
        await storage.RemoveItemAsync(SessionKey);
        OnChange?.Invoke();
    }

    private void ApplySession(AdminSession session)
    {
        IsAuthenticated = true;
        AccessToken = session.AccessToken;
        RefreshToken = session.RefreshToken;
        ExpiresAt = session.ExpiresAt;
        User = session.Employee;
        EmployeeId = session.Employee.Id;
        BranchId = session.BranchId;
        IsOwner = session.IsOwner;
        CompanyCode = session.CompanyCode;
    }

    private void ResetState()
    {
        IsAuthenticated = false;
        AccessToken = "";
        RefreshToken = "";
        ExpiresAt = null;
        User = null;
        EmployeeId = Guid.Empty;
        BranchId = Guid.Empty;
        IsOwner = false;
        CompanyCode = "";
    }
}

public record AdminSession(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    EmployeeInfo Employee,
    Guid BranchId,
    bool IsOwner,
    string CompanyCode);
