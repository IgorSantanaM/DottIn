using DottIn.Admin.Models;
using DottIn.Admin.Services;
using Microsoft.JSInterop;

namespace DottIn.Admin.Tests.Services;

public sealed class DashboardSessionCacheTests
{
    [Fact]
    public async Task SnapshotSurvivesReloadInSameTabAndIsScopedToAccount()
    {
        var browser = new BrowserStorage();
        var employeeId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var summary = Summary(DateTime.Today.AddHours(10));
        var original = new DashboardSessionCache(new SessionStorageService(browser));

        await original.SetAsync(employeeId, branchId, summary);
        var restored = new DashboardSessionCache(new SessionStorageService(browser));

        Assert.Equal(3, (await restored.GetAsync(employeeId, branchId))?.Summary.ActiveEmployees);
        Assert.Empty(browser.Local);
        Assert.Null(await restored.GetAsync(Guid.NewGuid(), branchId));
        Assert.Empty(browser.Session);
    }

    [Fact]
    public async Task OldOrDifferentDaySnapshotsAreDiscarded()
    {
        var browser = new BrowserStorage();
        var employeeId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var storage = new SessionStorageService(browser);
        var cache = new DashboardSessionCache(storage);

        await storage.SetSessionItemAsync(DashboardSessionCache.StorageKey,
            new DashboardSnapshot(employeeId, branchId, DateTime.UtcNow.AddMinutes(-6), Summary(DateTime.Today)));
        Assert.Null(await cache.GetAsync(employeeId, branchId));

        await storage.SetSessionItemAsync(DashboardSessionCache.StorageKey,
            new DashboardSnapshot(employeeId, branchId, DateTime.UtcNow.AddMinutes(-2),
                Summary(DateTime.Today.AddDays(-1).AddHours(23).AddMinutes(59))));
        Assert.Null(await cache.GetAsync(employeeId, branchId));
        Assert.Empty(browser.Session);
    }

    [Fact]
    public async Task RecentStaleSnapshotCanRenderWhileRefreshing()
    {
        var browser = new BrowserStorage();
        var employeeId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var storage = new SessionStorageService(browser);
        var cache = new DashboardSessionCache(storage);
        await storage.SetSessionItemAsync(DashboardSessionCache.StorageKey,
            new DashboardSnapshot(employeeId, branchId, DateTime.UtcNow.AddMinutes(-1),
                Summary(DateTime.Today.AddHours(10))));

        var snapshot = await cache.GetAsync(employeeId, branchId);

        Assert.NotNull(snapshot);
        Assert.True(DateTime.UtcNow - snapshot.SavedAtUtc > DashboardSessionCache.FreshFor);
    }

    [Fact]
    public async Task CorruptSnapshotIsRemoved()
    {
        var browser = new BrowserStorage();
        browser.Session[DashboardSessionCache.StorageKey] = "{invalid";
        var cache = new DashboardSessionCache(new SessionStorageService(browser));

        Assert.Null(await cache.GetAsync(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Empty(browser.Session);
    }

    [Fact]
    public async Task SuccessfulPointPunchInvalidatesSavedDashboard()
    {
        var browser = new BrowserStorage();
        var dashboardCache = new DashboardSessionCache(new SessionStorageService(browser));
        var employeeId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await dashboardCache.SetAsync(employeeId, branchId, Summary(DateTime.Today.AddHours(10)));
        using var http = new HttpClient(new SuccessHandler())
        {
            BaseAddress = new Uri("https://dottin.test")
        };
        var api = new AdminApiClient(http, new AdminQueryCache(), dashboardCache);

        await api.ClockInAsync(new ClockInRequest(branchId, employeeId, 0, 0));

        Assert.Empty(browser.Session);
    }

    private static DashboardSummary Summary(DateTime localNow)
        => new(3, [], null, DateTime.UtcNow, localNow, "America/Cuiaba");

    private sealed class SuccessHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/api/timekeeping/clock-in", request.RequestUri?.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));
        }
    }

    private sealed class BrowserStorage : IJSRuntime
    {
        public Dictionary<string, string> Session { get; } = new();
        public Dictionary<string, string> Local { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = (string)args![0]!;
            object? result = null;
            switch (identifier)
            {
                case "sessionStorage.getItem":
                    Session.TryGetValue(key, out var sessionValue);
                    result = sessionValue;
                    break;
                case "sessionStorage.setItem":
                    Session[key] = (string)args[1]!;
                    break;
                case "sessionStorage.removeItem":
                    Session.Remove(key);
                    break;
                case "localStorage.removeItem":
                    Local.Remove(key);
                    break;
                default:
                    throw new NotSupportedException(identifier);
            }

            return new ValueTask<TValue>((TValue)result!);
        }
    }
}
