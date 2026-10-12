using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DottIn.Admin.Models;
using DottIn.Admin.Services;
using Microsoft.JSInterop;

namespace DottIn.Admin.Tests.Services;

public sealed class SettingsTests
{
    [Fact]
    public async Task ThemeSurvivesRefreshAndIsIsolatedByAccount()
    {
        var browser = new BrowserStorage();
        var state = new AdminState(new(browser), new());
        var first = Session("Ana");
        var second = Session("Bruno");
        state.SetSystemDarkMode(false);
        await state.SetAuthenticatedAsync(first);
        Assert.False(state.IsDarkMode);
        await state.SetThemeAsync("dark");
        var restored = new AdminState(new(browser), new());
        Assert.True(await restored.RestoreSnapshotAsync());
        restored.SetSystemDarkMode(false);
        await restored.RestoreThemeAsync();
        Assert.True(restored.IsDarkMode);
        Assert.Equal("dark", restored.ThemePreference);
        await state.SetAuthenticatedAsync(second);
        Assert.False(state.IsDarkMode);
        await state.SetThemeAsync("system");
        state.SetSystemDarkMode(true);
        Assert.True(state.IsDarkMode);
        await state.SetThemeAsync("light");
        state.SetSystemDarkMode(true);
        Assert.False(state.IsDarkMode);
        await Assert.ThrowsAsync<ArgumentException>(() => state.SetThemeAsync("invalid"));
        await state.SetAuthenticatedAsync(first);
        Assert.Equal("dark", state.ThemePreference);
    }

    [Fact]
    public async Task SavedProfileUpdatesHeaderAndNonSensitiveSnapshotOnly()
    {
        var browser = new BrowserStorage();
        var state = new AdminState(new(browser), new());
        var session = Session("Ana");
        await state.SetAuthenticatedAsync(session);
        await state.UpdatePersonalSettingsAsync(new(session.Employee.Id, "Ana Silva", "12345678901", null, "version"));
        Assert.Equal("Ana Silva", state.User!.Name);
        Assert.DoesNotContain("12345678901", browser.Values["admin.session.snapshot"]);
        Assert.DoesNotContain("access-token", browser.Values["admin.session.snapshot"]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => state.UpdatePersonalSettingsAsync(new(Guid.NewGuid(), "Other", "", null, "v")));
        var restored = new AdminState(new(browser), new());
        await restored.RestoreSnapshotAsync();
        Assert.Equal("Ana Silva", restored.User!.Name);
    }

    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Administrator", true)]
    [InlineData("Manager", false)]
    [InlineData("Employee", false)]
    [InlineData("Accountant", false)]
    public async Task UnitSettingsUiHonorsRole(string role, bool allowed)
    {
        var state = new AdminState(new(new BrowserStorage()), new());
        await state.SetAuthenticatedAsync(Session("Test") with { Role = role });
        Assert.Equal(allowed, state.CanEditUnitSettings);
        await state.LogoutAsync();
        Assert.False(state.CanEditUnitSettings);
    }

    [Fact]
    public async Task BranchManagementCachesReadsAndCreationInvalidatesManagementOwnerAndBilling()
    {
        var ct = TestContext.Current.CancellationToken;
        var handler = new ApiHandler();
        var cache = new AdminQueryCache();
        var api = new AdminApiClient(new(handler) { BaseAddress = new("http://localhost") }, cache);
        await cache.GetOrCreateAsync("billing:subscription", TimeSpan.FromMinutes(5), () => Task.FromResult("old"), cancellationToken: ct);
        await cache.GetOrCreateAsync("owner:one:branches", TimeSpan.FromMinutes(5), () => Task.FromResult("old"), cancellationToken: ct);
        await api.GetBranchManagementAsync(cancellationToken: ct);
        await api.GetBranchManagementAsync(cancellationToken: ct);
        Assert.Equal(1, handler.Reads);
        await api.GetBranchManagementAsync(true, ct);
        Assert.Equal(2, handler.Reads);
        var request = new CreateManagedBranchRequest("Branch", new("11222333000181", "CNPJ"), new(-15, -56),
            new("Rua", 1, null, "Cuiabá", "MT", "78000000"), "America/Cuiaba", new(8, 0), new(17, 0),
            "test@example.invalid", "123", null, false, 100, 10);
        await api.CreateManagedBranchAsync(request, ct);
        await api.GetBranchManagementAsync(cancellationToken: ct);
        Assert.Equal(3, handler.Reads);
        Assert.Equal("new", await cache.GetOrCreateAsync("billing:subscription", TimeSpan.FromMinutes(5), () => Task.FromResult("new"), cancellationToken: ct));
        Assert.Equal("new", await cache.GetOrCreateAsync("owner:one:branches", TimeSpan.FromMinutes(5), () => Task.FromResult("new"), cancellationToken: ct));
        handler.FailWrite = true;
        await Assert.ThrowsAsync<ApiException>(() => api.CreateManagedBranchAsync(request, ct));
        await api.GetBranchManagementAsync(cancellationToken: ct);
        Assert.Equal(3, handler.Reads); // Failed writes do not evict successful reads.
    }

    [Fact]
    public async Task SettingsConflictIsShownAsBusinessMessage()
    {
        var api = new AdminApiClient(new(new ApiHandler { FailWrite = true }) { BaseAddress = new("http://localhost") }, new());
        var error = await Assert.ThrowsAsync<ApiException>(() => api.SavePersonalSettingsAsync(new("Ana", "stale"), TestContext.Current.CancellationToken));
        Assert.Equal("Recarregue a seção.", error.Message);
    }

    private static AdminSession Session(string name) => new("access-token", DateTime.UtcNow.AddMinutes(15),
        new(Guid.NewGuid(), name, "12345678901", null), Guid.NewGuid(), false, "TEST", "Employee");

    [Fact]
    public async Task UnitSettingsSaveInvalidatesOnlyDependentQueries()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = Guid.NewGuid();
        var cache = new AdminQueryCache();
        var dependent = new[] { $"branch:{id}:settings", $"branch:{id}:clock", $"branch:{id}:dashboard",
            $"branch:{id}:history:page1", $"branch:{id}:holiday-work:2026", "branch-management" };
        var independent = new[] { $"branch:{id}:employees", $"branch:{Guid.NewGuid()}:clock" };
        foreach (var key in dependent.Concat(independent))
            await cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(5), () => Task.FromResult("old"), cancellationToken: ct);
        using var client = new HttpClient(new SettingsHandler(id)) { BaseAddress = new("http://localhost") };
        await new AdminApiClient(client, cache).SaveUnitSettingsAsync(id, new("rules", "v1", AllowedRadiusMeters: 200, ToleranceMinutes: 10), ct);
        foreach (var key in dependent)
            Assert.Equal("new", await cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(5), () => Task.FromResult("new"), cancellationToken: ct));
        foreach (var key in independent)
            Assert.Equal("old", await cache.GetOrCreateAsync(key, TimeSpan.FromMinutes(5), () => Task.FromResult("new"), cancellationToken: ct));
    }

    private sealed class SettingsHandler(Guid id) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new UnitSettings(
                id, "Test", "11222333000181", "TEST", "test@example.invalid", null,
                new("Rua", 1, null, "Cuiabá", "MT", "78000000"), new(-15, -56), "America/Cuiaba",
                new(8, 0), new(17, 0), 200, 10, null, "v2")) });
    }

    private sealed class ApiHandler : HttpMessageHandler
    {
        public int Reads;
        public bool FailWrite;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Get)
            {
                Reads++;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new BranchManagement(Guid.NewGuid(), [], null)) });
            }
            return Task.FromResult(FailWrite
                ? new HttpResponseMessage(HttpStatusCode.Conflict) { Content = JsonContent.Create(new { message = "Recarregue a seção." }) }
                : new HttpResponseMessage(HttpStatusCode.Created) { Content = JsonContent.Create(new CreatedBranch(Guid.NewGuid(), "NEW")) });
        }
    }
    private sealed class BrowserStorage : IJSRuntime
    {
        public Dictionary<string, string> Values { get; } = [];
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => InvokeAsync<T>(identifier, CancellationToken.None, args);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = (string)args![0]!;
            object? value = null;
            if (identifier.EndsWith("getItem")) Values.TryGetValue(key, out var stored);
            else if (identifier.EndsWith("setItem")) Values[key] = (string)args[1]!;
            else if (identifier.EndsWith("removeItem")) Values.Remove(key);
            if (identifier.EndsWith("getItem")) value = Values.GetValueOrDefault(key);
            return ValueTask.FromResult((T)value!);
        }
    }
}
