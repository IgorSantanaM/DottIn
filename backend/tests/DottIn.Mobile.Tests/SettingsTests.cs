using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DottIn.Mobile.Services;
using DottIn.Mobile.Services.Interfaces;
using Refit;

namespace DottIn.Mobile.Tests;

public sealed class SettingsTests
{
    [Fact]
    public async Task AppearanceIsAccountScopedAndSurvivesLogout()
    {
        var storage = new Storage();
        var state = new AppState();
        var first = new EmployeeInfo(Guid.NewGuid(), "Ana", "", null);
        var second = new EmployeeInfo(Guid.NewGuid(), "Bruno", "", null);
        state.SetAuthenticated(first, Guid.NewGuid());
        var appearance = new MobileAppearanceService(storage, state);
        await appearance.SetAsync("dark", false);
        state.Logout();
        state.SetAuthenticated(second, Guid.NewGuid());
        await appearance.RestoreAsync(false);
        Assert.False(state.IsDarkMode);
        Assert.Equal("system", state.ThemePreference);
        state.SetAuthenticated(first, Guid.NewGuid());
        await appearance.RestoreAsync(false);
        Assert.True(state.IsDarkMode);
        await appearance.SetAsync("system", false);
        state.SetTheme(state.ThemePreference, true);
        Assert.True(state.IsDarkMode);
        await Assert.ThrowsAsync<ArgumentException>(() => appearance.SetAsync("invalid", false));
    }

    [Fact]
    public async Task ProfileSaveRetainsCurrentTokensAndUpdatesPersistedSession()
    {
        using var client = ApiContractTests.Client(_ => new(HttpStatusCode.OK));
        var storage = new Storage(); var state = new AppState();
        var session = new MobileSessionService(RestService.For<IAuthApi>(client), RestService.For<IBranchApi>(client), storage, state);
        var employee = new EmployeeInfo(Guid.NewGuid(), "Ana", "", null);
        await session.AcceptAsync(new("access", "refresh", DateTime.UtcNow.AddHours(1), employee, Guid.NewGuid(), false, false, "TEST", "Employee"));
        await new MobileAppearanceService(storage, state).SetAsync("dark", false);
        await session.UpdateProfileAsync(new(employee.Id, "Ana Silva", "", null, "v"));
        Assert.Equal("Ana Silva", state.Employee!.Name);
        Assert.Equal("Ana Silva", JsonSerializer.Deserialize<LoginResponse>((await storage.GetAsync("mobile_session"))!)!.Employee.Name);
        Assert.Equal("access", await storage.GetAsync("access_token"));
        await session.ClearAsync();
        Assert.Null(await storage.GetAsync("access_token"));
        Assert.Equal("dark", await storage.GetAsync($"theme:{employee.Id}"));
    }

    [Theory]
    [InlineData("/profile")]
    [InlineData("/settings")]
    public void PersonalSettingsAreAvailableBeforeCompanySetup(string path)
    {
        Assert.Null(MobileRoutePolicy.Redirect(path, true, true, Guid.Empty));
        Assert.Equal("/login", MobileRoutePolicy.Redirect(path, false, true, Guid.Empty));
        Assert.Equal("/onboarding/company", MobileRoutePolicy.Redirect("/settings/unit", true, true, Guid.Empty));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EndingAllSessionsOnlyClearsLocalSessionAfterServerConfirmation(bool success)
    {
        using var client = ApiContractTests.Client(_ => new(success ? HttpStatusCode.NoContent : HttpStatusCode.ServiceUnavailable));
        var storage = new Storage(); var state = new AppState();
        var session = new MobileSessionService(RestService.For<IAuthApi>(client), RestService.For<IBranchApi>(client), storage, state);
        await session.AcceptAsync(new("access", "refresh", DateTime.UtcNow.AddHours(1), new(Guid.NewGuid(), "Test", "", null), Guid.NewGuid(), false, false));
        if (success) await session.EndAllSessionsAsync();
        else await Assert.ThrowsAsync<Refit.ApiException>(() => session.EndAllSessionsAsync());
        Assert.Equal(!success, state.IsAuthenticated);
        Assert.Equal(success ? null : "access", await storage.GetAsync("access_token"));
    }

    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Administrator", true)]
    [InlineData("Manager", false)]
    [InlineData("Employee", false)]
    public void UnitSettingsVisibilityUsesRoleNotLegacyFlag(string role, bool allowed)
    {
        var state = new AppState();
        state.SetAuthenticated(new(Guid.NewGuid(), "Test", "", null), Guid.NewGuid(), isOwner: true, role: role);
        Assert.Equal(allowed, state.CanEditUnitSettings);
        Assert.Equal(role == "Owner", state.IsOwner);
    }

    [Fact]
    public async Task ApiContractsUseCurrentIdentityAndSendConcurrencyVersion()
    {
        string? path = null, body = null;
        using var client = ApiContractTests.Client(request => {
            path = request.RequestUri!.AbsolutePath;
            body = request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new PersonalSettings(Guid.NewGuid(), "Test", "", null, "next")) };
        });
        await RestService.For<ISettingsApi>(client).SavePersonalAsync(new("Test", "expected-version"));
        Assert.Equal("/api/settings/me", path);
        Assert.Contains("expected-version", body);
        Assert.DoesNotContain("employeeId", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Storage : ISecureStorageService
    {
        private readonly Dictionary<string, string> values = [];
        public Task<string?> GetAsync(string key) => Task.FromResult(values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { values[key] = value; return Task.CompletedTask; }
        public Task RemoveAsync(string key) { values.Remove(key); return Task.CompletedTask; }
        public Task ClearAllAsync() { values.Clear(); return Task.CompletedTask; }
    }
}
