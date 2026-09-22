using DottIn.Admin.Models;
using System.Text.Json;

namespace DottIn.Admin.Services;

public class AdminState(SessionStorageService storage, AdminQueryCache cache)
{
    private const string SessionKey = "admin.session.snapshot";
    private const string LegacySessionKey = "admin.session";
    private AdminSession? currentSession;

    public bool IsAuthenticated { get; private set; }
    public bool SessionPersistenceAvailable => storage.IsPersistent;
    public int SessionVersion { get; private set; }
    public bool IsSessionReady { get; private set; }
    public bool IsSessionRestoring => IsAuthenticated && !IsSessionReady;
    public string? SessionError { get; private set; }
    public string AccessToken { get; private set; } = "";
    public DateTime? ExpiresAt { get; private set; }
    public EmployeeInfo? User { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid BranchId { get; private set; }
    public bool IsOwner { get; private set; }
    public string Role { get; private set; } = "Employee";
    public bool CanViewBranchRecords => Role is "Owner" or "Administrator";
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
        // Remove the former token-bearing session even when a snapshot already exists.
        await storage.RemoveItemAsync(LegacySessionKey);

        AdminSessionSnapshot? snapshot;
        try
        {
            snapshot = await storage.GetItemAsync<AdminSessionSnapshot>(SessionKey);
        }
        catch (JsonException)
        {
            await storage.RemoveItemAsync(SessionKey);
            return false;
        }

        if (snapshot?.Employee is null || snapshot.Employee.Id == Guid.Empty)
        {
            await storage.RemoveItemAsync(SessionKey);
            return false;
        }

        if (!string.IsNullOrEmpty(snapshot.Employee.Cpf))
        {
            snapshot = snapshot with { Employee = snapshot.Employee with { Cpf = string.Empty } };
            await storage.SetItemAsync(SessionKey, snapshot);
        }

        cache.Clear();
        ApplySnapshot(snapshot);
        SessionVersion++;
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
            response.CompanyCode,
            response.Role ?? (response.IsOwner ? "Owner" : "Employee"));

        await SetAuthenticatedAsync(session);
    }

    public async Task SetAuthenticatedAsync(AdminSession session)
    {
        cache.Clear();
        currentSession = session;
        SessionVersion++;
        ApplySession(session);
        await storage.RemoveItemAsync(DashboardSessionCache.StorageKey);
        await PersistSnapshotAsync();
        OnChange?.Invoke();
    }

    public async Task CompleteRefreshAsync(RefreshTokenResponse response)
    {
        if (User is null)
            throw new InvalidOperationException("Não há sessão local para renovar.");

        Role = response.Role ?? Role;
        currentSession = new AdminSession(
            response.AccessToken,
            response.ExpiresAt,
            User,
            BranchId,
            IsOwner,
            CompanyCode,
            Role);

        AccessToken = response.AccessToken;
        ExpiresAt = response.ExpiresAt;
        IsAuthenticated = true;
        IsSessionReady = true;
        SessionError = null;
        await PersistSnapshotAsync();
        OnChange?.Invoke();
    }

    public Task<AdminSession?> GetSessionAsync()
        => Task.FromResult(currentSession);

    public async Task LogoutAsync()
    {
        cache.Clear();
        SessionVersion++;
        ResetState();
        await storage.RemoveItemAsync(SessionKey);
        await storage.RemoveItemAsync(LegacySessionKey);
        await storage.RemoveItemAsync(DashboardSessionCache.StorageKey);
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
            User with { Cpf = string.Empty },
            BranchId,
            IsOwner,
            CompanyCode,
            Role));
    }

    private void ApplySession(AdminSession session)
    {
        ApplySnapshot(new AdminSessionSnapshot(
            session.Employee,
            session.BranchId,
            session.IsOwner,
            session.CompanyCode,
            session.Role));
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
        Role = snapshot.Role;
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
        Role = "Employee";
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
    string CompanyCode,
    string Role = "Employee");

public record AdminSessionSnapshot(
    EmployeeInfo Employee,
    Guid BranchId,
    bool IsOwner,
    string CompanyCode,
    string Role = "Employee");