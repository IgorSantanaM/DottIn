using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using DottIn.Mobile.Services;
using DottIn.Mobile.Services.Interfaces;

namespace DottIn.Mobile.Tests;

public sealed class MobileTokenRefreshTests
{
    [Fact]
    public async Task NearExpiry_ConcurrentRequestsRotateOnlyOnce()
    {
        var storage = await SeedAsync(DateTime.UtcNow.AddSeconds(30));
        var refreshes = 0;
        var factory = new StubFactory(_ =>
        {
            Interlocked.Increment(ref refreshes);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse(
                    "renewed", "next-refresh", DateTime.UtcNow.AddMinutes(15)))
            };
        });
        var tokens = new MobileTokenRefreshService(storage, factory, new AppState());

        var results = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => tokens.GetAccessTokenAsync(CancellationToken.None)));

        Assert.All(results, token => Assert.Equal("renewed", token));
        Assert.Equal(1, refreshes);
        Assert.Equal("next-refresh", await storage.GetAsync("refresh_token"));
    }

    [Fact]
    public async Task ProtectedRead_RefreshesBeforeSendingAndUsesNewToken()
    {
        var storage = await SeedAsync(DateTime.UtcNow.AddSeconds(30));
        var factory = new StubFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new TokenResponse(
                "renewed", "next-refresh", DateTime.UtcNow.AddMinutes(15)))
        });
        var tokens = new MobileTokenRefreshService(storage, factory, new AppState());
        using var client = new HttpClient(new AuthorizationHandler(tokens)
        {
            InnerHandler = new StubHandler(request =>
            {
                Assert.Equal("renewed", request.Headers.Authorization?.Parameter);
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        }) { BaseAddress = new Uri("http://localhost:5101") };

        using var response = await client.GetAsync("/api/branches", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedRead_RetriesOnceAfterUnauthorized()
    {
        var storage = await SeedAsync(DateTime.UtcNow.AddMinutes(15));
        var refreshes = 0;
        var factory = new StubFactory(_ =>
        {
            Interlocked.Increment(ref refreshes);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new TokenResponse(
                    "renewed", "next-refresh", DateTime.UtcNow.AddMinutes(15)))
            };
        });
        var tokens = new MobileTokenRefreshService(storage, factory, new AppState());
        var sends = 0;
        using var client = new HttpClient(new AuthorizationHandler(tokens)
        {
            InnerHandler = new StubHandler(request =>
            {
                Interlocked.Increment(ref sends);
                return new HttpResponseMessage(
                    request.Headers.Authorization?.Parameter == "old"
                        ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
            })
        }) { BaseAddress = new Uri("http://localhost:5101") };

        using var response = await client.GetAsync("/api/branches", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, sends);
        Assert.Equal(1, refreshes);
    }
    [Fact]
    public async Task RejectedRefresh_ClearsCredentialsWithoutRetryingMutation()
    {
        var storage = await SeedAsync(DateTime.UtcNow.AddSeconds(30));
        await storage.SetAsync("mobile_session", "saved-session");
        var factory = new StubFactory(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var tokens = new MobileTokenRefreshService(storage, factory, new AppState());
        var sends = 0;
        using var client = new HttpClient(new AuthorizationHandler(tokens)
        {
            InnerHandler = new StubHandler(_ =>
            {
                Interlocked.Increment(ref sends);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            })
        }) { BaseAddress = new Uri("http://localhost:5101") };

        using var response = await client.PostAsync("/api/timekeeping/clock-in", JsonContent.Create(new { }),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, sends);
        Assert.Null(await storage.GetAsync("access_token"));
        Assert.Null(await storage.GetAsync("refresh_token"));
        Assert.Null(await storage.GetAsync("mobile_session"));
    }

    private static async Task<MemoryStorage> SeedAsync(DateTime expiry)
    {
        var storage = new MemoryStorage();
        await storage.SetAsync("access_token", "old");
        await storage.SetAsync("refresh_token", "refresh");
        await storage.SetAsync(MobileTokenRefreshService.ExpirationKey, expiry.ToString("O"));
        return storage;
    }

    private sealed class MemoryStorage : ISecureStorageService
    {
        private readonly ConcurrentDictionary<string, string> values = new();
        public Task<string?> GetAsync(string key) => Task.FromResult(values.GetValueOrDefault(key));
        public Task SetAsync(string key, string value) { values[key] = value; return Task.CompletedTask; }
        public Task RemoveAsync(string key) { values.TryRemove(key, out _); return Task.CompletedTask; }
        public Task ClearAllAsync() { values.Clear(); return Task.CompletedTask; }
    }

    private sealed class StubFactory(Func<HttpRequestMessage, HttpResponseMessage> send) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(send))
        {
            BaseAddress = new Uri("http://localhost:5101")
        };
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(send(request));
    }
}
