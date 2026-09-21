using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

public class AdminState(SessionStorageService storage)
{
    private const string SessionKey = "admin.session.snapshot";
    private const string LegacySessionKey = "admin.session";
    private AdminSession? currentSession;

    public bool IsAuthenticated { get; private set; }
    public bool IsSessionReady { get; private set; }
    public bool IsSessionRestoring => IsAuthenticated && !IsSessionReady;
    public string? SessionError { get; private set; }
    public string AccessToken { get; private set; } = "";
    public DateTime? ExpiresAt { get; private set; }
    public EmployeeInfo? User { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid BranchId { get; private set; }
    public bool IsOwner { get; private set; }
    public bool IsDarkMode { get; private set; } = true;
    public string CompanyCode { get; private set; } = "";
    public bool HasCompletedConfiguration => BranchId != Guid.Empty;
    public bool HasLinkedPlan { get; private set; }
    public bool IsOperationalAccessResolved { get; private set; }
    public bool CanAccessOperationalModules => OperationalAccessPolicy.CanAccessModules(
        BranchId,
        HasLinkedPlan,
        IsOperationalAccessResolved);

    public event Action? OnChange;

    public void ToggleDarkMode()
    {
        IsDarkMode = !IsDarkMode;
        OnChange?.Invoke();
    }

    public async Task<bool> RestoreSnapshotAsync()
    {
        var snapshot = await storage.GetItemAsync<AdminSessionSnapshot>(SessionKey);
        if (snapshot is null)
        {
            await storage.RemoveItemAsync(LegacySessionKey);
            return false;
        }

        ApplySnapshot(snapshot);
        IsAuthenticated = true;
        IsSessionReady = false;
        OnChange?.Invoke();
        return true;
    }

    public void MarkSessionRestoreFailed(string message)
    {
        SessionError = message;
        OnChange?.Invoke();
    }
    public async Task SetAuthenticatedAsync(LoginResponse response)
    {
        var session = new AdminSession(
            response.AccessToken,
            response.ExpiresAt,
            response.Employee,
            response.BranchId,
            response.IsOwner,
            response.CompanyCode);

        await SetAuthenticatedAsync(session);
    }

    public async Task SetAuthenticatedAsync(AdminSession session)
    {
        currentSession = session;
        ApplySession(session);
        await PersistSnapshotAsync();
        OnChange?.Invoke();
    }

    public Task CompleteRefreshAsync(RefreshTokenResponse response)
    {
        if (User is null)
            throw new InvalidOperationException("Não há sessão local para renovar.");

        currentSession = new AdminSession(
            response.AccessToken,
            response.ExpiresAt,
            User,
            BranchId,
            IsOwner,
            CompanyCode);

        AccessToken = response.AccessToken;
        ExpiresAt = response.ExpiresAt;
        IsAuthenticated = true;
        IsSessionReady = true;
        SessionError = null;
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public Task<AdminSession?> GetSessionAsync()
        => Task.FromResult(currentSession);

    public async Task LogoutAsync()
    {
        ResetState();
        await storage.RemoveItemAsync(SessionKey);
        await storage.RemoveItemAsync(LegacySessionKey);
        OnChange?.Invoke();
    }

    public void SetOperationalAccess(bool hasLinkedPlan)
    {
        HasLinkedPlan = hasLinkedPlan;
        IsOperationalAccessResolved = true;
        OnChange?.Invoke();
    }

    private async Task PersistSnapshotAsync()
    {
        if (User is null)
            return;

        await storage.SetItemAsync(SessionKey, new AdminSessionSnapshot(
            User,
            BranchId,
            IsOwner,
            CompanyCode));
    }

    private void ApplySession(AdminSession session)
    {
        ApplySnapshot(new AdminSessionSnapshot(
            session.Employee,
            session.BranchId,
            session.IsOwner,
            session.CompanyCode));
        IsAuthenticated = true;
        IsSessionReady = true;
        AccessToken = session.AccessToken;
        ExpiresAt = session.ExpiresAt;
    }

    private void ApplySnapshot(AdminSessionSnapshot snapshot)
    {
        User = snapshot.Employee;
        EmployeeId = snapshot.Employee.Id;
        BranchId = snapshot.BranchId;
        IsOwner = snapshot.IsOwner;
        CompanyCode = snapshot.CompanyCode;
        HasLinkedPlan = false;
        IsOperationalAccessResolved = snapshot.BranchId == Guid.Empty;
    }

    private void ResetState()
    {
        currentSession = null;
        IsAuthenticated = false;
        IsSessionReady = false;
        SessionError = null;
        AccessToken = "";
        ExpiresAt = null;
        User = null;
        EmployeeId = Guid.Empty;
        BranchId = Guid.Empty;
        IsOwner = false;
        CompanyCode = "";
        HasLinkedPlan = false;
        IsOperationalAccessResolved = false;
    }
}

public record AdminSession(
    string AccessToken,
    DateTime ExpiresAt,
    EmployeeInfo Employee,
    Guid BranchId,
    bool IsOwner,
    string CompanyCode);

public record AdminSessionSnapshot(
    EmployeeInfo Employee,
    Guid BranchId,
    bool IsOwner,
    string CompanyCode);