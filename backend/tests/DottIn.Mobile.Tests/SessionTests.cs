using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DottIn.Mobile.Services;
using DottIn.Mobile.Services.Interfaces;
using Refit;

namespace DottIn.Mobile.Tests;

public class SessionTests
{
    [Fact]
    public async Task Owner_without_company_can_resume_after_restart()
    {
        var storage = new MemoryStorage();
        using var client = ApiContractTests.Client(request => request.RequestUri!.AbsolutePath.EndsWith("/refresh")
            ? new(HttpStatusCode.OK) { Content = JsonContent.Create(new TokenResponse("renewed", "new-refresh", DateTime.UtcNow.AddHours(1))) }
            : new(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<BranchSummary>()) });
        var first = Service(client, storage, new AppState());
        await first.AcceptAsync(Owner());
        var restoredState = new AppState();
        var restored = Service(client, storage, restoredState);
        await restored.RestoreAsync();
        Assert.True(restoredState.IsAuthenticated);
        Assert.True(restoredState.IsOwner);
        Assert.Equal("/onboarding/company", restored.LandingPage);
        Assert.Equal("renewed", await storage.GetAsync("access_token"));
    }

    [Fact]
    public async Task Switching_branch_updates_company_code_and_persisted_session()
    {
        var headquarters = Guid.NewGuid(); var branch = Guid.NewGuid();
        var storage = new MemoryStorage(); var state = new AppState();
        using var client = ApiContractTests.Client(request => request.RequestUri!.AbsolutePath.Contains("/owner/")
            ? new(HttpStatusCode.OK) { Content = JsonContent.Create(new[] { new BranchSummary(headquarters, "HQ", true, true), new BranchSummary(branch, "Branch", true, false) }) }
            : new(HttpStatusCode.OK) { Content = JsonContent.Create(new BranchContext("Company", request.RequestUri.AbsolutePath.EndsWith(branch.ToString()) ? "BRANCH" : "HQ")) });
        var service = Service(client, storage, state);
        await service.AcceptAsync(Owner());
        await service.SwitchBranchAsync(branch);
        Assert.Equal(branch, state.BranchId);
        Assert.Equal("Branch", state.SelectedBranchName);
        Assert.Equal("BRANCH", await storage.GetAsync("company_code"));
        Assert.Equal(branch, JsonSerializer.Deserialize<LoginResponse>((await storage.GetAsync("mobile_session"))!)!.BranchId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SwitchBranchAsync(Guid.NewGuid()));
        Assert.Equal(branch, state.BranchId);
    }

    [Fact]
    public async Task Expired_refresh_clears_session()
    {
        var storage = new MemoryStorage();
        await storage.SetAsync("mobile_session", JsonSerializer.Serialize(Owner()));
        await storage.SetAsync("refresh_token", "expired");
        using var client = ApiContractTests.Client(_ => new(HttpStatusCode.Unauthorized));
        var state = new AppState();
        await Service(client, storage, state).RestoreAsync();
        Assert.False(state.IsAuthenticated);
        Assert.Null(await storage.GetAsync("mobile_session"));
    }

    [Fact]
    public async Task Remember_me_false_does_not_restore_on_next_launch()
    {
        var storage = new MemoryStorage();
        using var client = ApiContractTests.Client(_ => new(HttpStatusCode.OK));
        var response = Owner() with { IsOwner = false, BranchId = Guid.NewGuid(), CompanyCode = "ACME" };
        var state = new AppState();
        await Service(client, storage, state).AcceptAsync(response, remember: false);
        Assert.True(state.IsAuthenticated);
        Assert.Null(await storage.GetAsync("mobile_session"));
        var next = new AppState();
        await Service(client, storage, next).RestoreAsync();
        Assert.False(next.IsAuthenticated);
        Assert.Null(await storage.GetAsync("access_token"));
        Assert.Null(await storage.GetAsync("refresh_token"));
    }

    private static LoginResponse Owner() => new("access", "refresh", DateTime.UtcNow.AddHours(1), new(Guid.NewGuid(), "Test Owner", "12345678901", null), Guid.Empty, true, false);
    private static MobileSessionService Service(HttpClient client, MemoryStorage storage, AppState state)
        => new(RestService.For<IAuthApi>(client), RestService.For<IBranchApi>(client), storage, state);
    private sealed class MemoryStorage : ISecureStorageService
    {
        private readonly Dictionary<string, string> _values = [];
        public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { _values[key] = value; return Task.CompletedTask; }
        public Task RemoveAsync(string key) { _values.Remove(key); return Task.CompletedTask; }
        public Task ClearAllAsync() { _values.Clear(); return Task.CompletedTask; }
    }
}
