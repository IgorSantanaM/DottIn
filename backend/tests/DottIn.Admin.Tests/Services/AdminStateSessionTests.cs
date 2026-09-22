using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DottIn.Admin.Models;
using DottIn.Admin.Services;
using Microsoft.JSInterop;

namespace DottIn.Admin.Tests.Services;

public sealed class AdminStateSessionTests
{
    [Fact]
    public async Task LoginPersistsOnlyNonSensitiveSnapshotAndClearsPreviousAccountCache()
    {
        var browser = new MemoryBrowserStorage();
        var cache = new AdminQueryCache();
        await cache.GetOrCreateAsync("branch:one:dashboard", TimeSpan.FromMinutes(1), () => Task.FromResult("previous account"), cancellationToken: TestContext.Current.CancellationToken);
        var state = new AdminState(new SessionStorageService(browser), cache);
        var employee = new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "12345678901", null);

        await state.SetAuthenticatedAsync(new AdminSession(
            "secret-access-token", DateTime.UtcNow.AddMinutes(15), employee, Guid.NewGuid(), true, "COMP"));

        var snapshot = browser.Local["admin.session.snapshot"];
        Assert.DoesNotContain("secret-access-token", snapshot, StringComparison.Ordinal);
        Assert.DoesNotContain(employee.Cpf, snapshot, StringComparison.Ordinal);
        Assert.Equal("new account", await cache.GetOrCreateAsync("branch:one:dashboard", TimeSpan.FromMinutes(1), () => Task.FromResult("new account"), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(employee.Cpf, state.User?.Cpf);
    }

    [Fact]
    public async Task LoginAndLogoutClearTabScopedDashboardData()
    {
        var browser = new MemoryBrowserStorage();
        browser.Session[DashboardSessionCache.StorageKey] = "previous-account-dashboard";
        var state = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        var employee = new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "", null);

        await state.SetAuthenticatedAsync(new AdminSession(
            "access-token", DateTime.UtcNow.AddMinutes(15), employee, Guid.NewGuid(), true, "COMP"));
        Assert.False(browser.Session.ContainsKey(DashboardSessionCache.StorageKey));

        browser.Session[DashboardSessionCache.StorageKey] = "current-dashboard";
        await state.LogoutAsync();
        Assert.False(browser.Session.ContainsKey(DashboardSessionCache.StorageKey));
    }
    [Fact]
    public async Task RestoreRemovesLegacyTokenAndScrubsPreviousCpfSnapshot()
    {
        var browser = new MemoryBrowserStorage();
        var employee = new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "12345678901", null);
        browser.Local["admin.session.snapshot"] = JsonSerializer.Serialize(new AdminSessionSnapshot(employee, Guid.NewGuid(), true, "COMP"));
        browser.Session["admin.session"] = "legacy-access-token";
        var state = new AdminState(new SessionStorageService(browser), new AdminQueryCache());

        Assert.True(await state.RestoreSnapshotAsync());

        Assert.False(browser.Session.ContainsKey("admin.session"));
        Assert.Equal(string.Empty, state.User?.Cpf);
        Assert.DoesNotContain(employee.Cpf, browser.Local["admin.session.snapshot"], StringComparison.Ordinal);
        Assert.True(state.IsSessionRestoring);
        Assert.Empty(state.AccessToken);
    }

    [Fact]
    public async Task CorruptSnapshotIsDiscardedWithoutAuthenticating()
    {
        var browser = new MemoryBrowserStorage();
        browser.Local["admin.session.snapshot"] = "not json";
        var state = new AdminState(new SessionStorageService(browser), new AdminQueryCache());

        Assert.False(await state.RestoreSnapshotAsync());
        Assert.False(state.IsAuthenticated);
        Assert.False(browser.Local.ContainsKey("admin.session.snapshot"));
    }

    [Fact]
    public async Task BlockedLocalStorageKeepsCurrentSessionUsableWithoutPersistence()
    {
        var browser = new MemoryBrowserStorage { BlockLocalStorage = true };
        browser.Session["admin.session"] = "legacy-access-token";
        var storage = new SessionStorageService(browser);
        var state = new AdminState(storage, new AdminQueryCache());
        var employee = new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "12345678901", null);

        Assert.False(await state.RestoreSnapshotAsync());
        Assert.False(browser.Session.ContainsKey("admin.session"));

        await state.SetAuthenticatedAsync(new AdminSession(
            "access-token", DateTime.UtcNow.AddMinutes(15), employee, Guid.NewGuid(), true, "COMP"));

        Assert.True(state.IsAuthenticated);
        Assert.True(state.IsSessionReady);
        Assert.False(state.SessionPersistenceAvailable);
        Assert.NotNull(await storage.GetItemAsync<AdminSessionSnapshot>("admin.session.snapshot"));

        await state.LogoutAsync();

        Assert.False(state.IsAuthenticated);
        Assert.Null(await storage.GetItemAsync<AdminSessionSnapshot>("admin.session.snapshot"));
        Assert.Empty(browser.Local);
    }

    [Fact]
    public async Task RestoredValidationAndExplicitRefreshShareOneRequest()
    {
        var browser = new MemoryBrowserStorage();
        var state = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        await state.SetAuthenticatedAsync(new AdminSession(
            "old-token", DateTime.UtcNow.AddMinutes(1),
            new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "", null),
            Guid.NewGuid(), true, "COMP"));

        var handler = new DelayedRefreshHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://dottin.test") };
        var auth = new AuthService(http, state);

        var validation = auth.ValidateRestoredSessionAsync(TestContext.Current.CancellationToken);
        await handler.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        var concurrentRefresh = auth.RefreshIfNeededAsync(force: true, TestContext.Current.CancellationToken);
        handler.Release.SetResult();

        await validation;
        Assert.True(await concurrentRefresh);
        Assert.Equal(1, handler.Calls);
        Assert.Equal("new-token", state.AccessToken);
    }

    [Fact]
    public async Task RefreshResponseCannotRestoreSessionAfterLogout()
    {
        var state = new AdminState(new SessionStorageService(new MemoryBrowserStorage()), new AdminQueryCache());
        await state.SetAuthenticatedAsync(new AdminSession(
            "old-token", DateTime.UtcNow.AddMinutes(1),
            new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "", null),
            Guid.NewGuid(), true, "COMP"));

        var handler = new DelayedRefreshHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://dottin.test") };
        var auth = new AuthService(http, state);

        var refresh = auth.RefreshIfNeededAsync(force: true, TestContext.Current.CancellationToken);
        await handler.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await state.LogoutAsync();
        handler.Release.SetResult();

        Assert.False(await refresh);
        Assert.False(state.IsAuthenticated);
        Assert.Empty(state.AccessToken);
    }

    [Fact]
    public async Task MalformedRefreshResponseKeepsRestoredSessionRetryable()
    {
        var browser = new MemoryBrowserStorage();
        var original = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        await original.SetAuthenticatedAsync(new AdminSession(
            "old-token", DateTime.UtcNow.AddMinutes(1),
            new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "", null),
            Guid.NewGuid(), true, "COMP"));

        var state = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        Assert.True(await state.RestoreSnapshotAsync());

        var handler = new DelayedRefreshHandler { ReturnMalformed = true };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://dottin.test") };
        var auth = new AuthService(http, state);

        var validation = auth.ValidateRestoredSessionAsync(TestContext.Current.CancellationToken);
        await handler.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        handler.Release.SetResult();
        await validation;

        Assert.True(state.IsSessionRestoring);
        Assert.NotNull(state.SessionError);
        Assert.True(state.IsAuthenticated);
    }
    [Fact]
    public async Task ReloadedSessionValidatesBeforeShowingAuthenticatedPages()
    {
        var browser = new MemoryBrowserStorage();
        var original = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        await original.SetAuthenticatedAsync(new AdminSession(
            "old-token", DateTime.UtcNow.AddMinutes(1),
            new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "", null),
            Guid.NewGuid(), true, "COMP"));

        var restored = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        Assert.True(await restored.RestoreSnapshotAsync());
        Assert.True(restored.IsSessionRestoring);
        Assert.Empty(restored.AccessToken);

        var handler = new DelayedRefreshHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://dottin.test") };
        var validation = new AuthService(http, restored).ValidateRestoredSessionAsync(TestContext.Current.CancellationToken);
        await handler.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        handler.Release.SetResult();
        await validation;

        Assert.True(handler.PersistenceHeaderSeen);
        Assert.True(restored.IsSessionReady);
        Assert.False(restored.IsSessionRestoring);
        Assert.Equal("new-token", restored.AccessToken);
    }
    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Administrator", true)]
    [InlineData("Manager", false)]
    [InlineData("Employee", false)]
    public async Task BranchHistoryAccessFollowsAuthenticatedRoleAfterReload(string role, bool expected)
    {
        var browser = new MemoryBrowserStorage();
        var original = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        await original.SetAuthenticatedAsync(new LoginResponse(
            "old-token", "refresh-token", DateTime.UtcNow.AddMinutes(1),
            new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "", null),
            Guid.NewGuid(), false, false, "COMP", role));

        var restored = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        Assert.True(await restored.RestoreSnapshotAsync());
        Assert.Equal(expected, restored.CanViewBranchRecords);

        await restored.CompleteRefreshAsync(new RefreshTokenResponse(
            "new-token", "refresh-token", DateTime.UtcNow.AddMinutes(15), role));
        Assert.Equal(expected, restored.CanViewBranchRecords);
        var snapshot = JsonSerializer.Deserialize<AdminSessionSnapshot>(browser.Local["admin.session.snapshot"]);
        Assert.Equal(role, snapshot?.Role);
    }
    [Fact]
    public async Task RefreshRemovesBranchHistoryAccessWhenRoleIsDowngraded()
    {
        var browser = new MemoryBrowserStorage();
        var state = new AdminState(new SessionStorageService(browser), new AdminQueryCache());
        await state.SetAuthenticatedAsync(new LoginResponse(
            "old-token", "refresh-token", DateTime.UtcNow.AddMinutes(1),
            new EmployeeInfo(Guid.NewGuid(), "Ana Silva", "", null),
            Guid.NewGuid(), false, false, "COMP", "Administrator"));
        Assert.True(state.CanViewBranchRecords);

        await state.CompleteRefreshAsync(new RefreshTokenResponse(
            "new-token", "next-refresh-token", DateTime.UtcNow.AddMinutes(15), "Employee"));

        Assert.False(state.CanViewBranchRecords);
        var snapshot = JsonSerializer.Deserialize<AdminSessionSnapshot>(browser.Local["admin.session.snapshot"]);
        Assert.Equal("Employee", snapshot?.Role);
    }
    private sealed class DelayedRefreshHandler : HttpMessageHandler
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls { get; private set; }
        public bool ReturnMalformed { get; set; }
        public bool PersistenceHeaderSeen { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("/api/auth/refresh", request.RequestUri?.AbsolutePath);
            PersistenceHeaderSeen = request.Headers.TryGetValues("X-DottIn-Persist-Session", out var values) && values.Contains("true");
            Calls++;
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            if (ReturnMalformed)
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{invalid") };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RefreshTokenResponse(
                    "new-token", "new-refresh-token", DateTime.UtcNow.AddMinutes(15)))
            };
        }
    }
    private sealed class MemoryBrowserStorage : IJSRuntime
    {
        public Dictionary<string, string> Local { get; } = new();
        public Dictionary<string, string> Session { get; } = new();
        public bool BlockLocalStorage { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (BlockLocalStorage && identifier.StartsWith("localStorage.", StringComparison.Ordinal))
                throw new JSException("Storage access blocked");

            var key = (string)args![0]!;
            object? result = null;
            switch (identifier)
            {
                case "localStorage.getItem":
                    Local.TryGetValue(key, out var value);
                    result = value;
                    break;
                case "localStorage.setItem":
                    Local[key] = (string)args[1]!;
                    break;
                case "localStorage.removeItem":
                    Local.Remove(key);
                    break;
                case "sessionStorage.removeItem":
                    Session.Remove(key);
                    break;
                default:
                    throw new NotSupportedException(identifier);
            }
            return new ValueTask<TValue>((TValue)result!);
        }
    }
}