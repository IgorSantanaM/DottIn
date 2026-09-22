using DottIn.Admin.Services;

namespace DottIn.Admin.Tests.Services;

public sealed class AdminQueryCacheTests
{
    [Fact]
    public async Task ConcurrentRequestsShareOneQueryAndReuseItsResult()
    {
        var cache = new AdminQueryCache();
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        Task<int> Factory()
        {
            calls++;
            return source.Task;
        }

        var first = Read(cache, "branch:one", Factory);
        var second = Read(cache, "branch:one", Factory);
        Assert.Equal(1, calls);

        source.SetResult(42);
        Assert.Equal(42, await first);
        Assert.Equal(42, await second);
        Assert.Equal(42, await Read(cache, "branch:one", Factory));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task InvalidatedQueryCannotReplaceFreshResult()
    {
        var cache = new AdminQueryCache();
        var oldSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stale = Read(cache, "branch:one:history:today", () => oldSource.Task);

        cache.Invalidate("branch:one:history:");
        var fresh = await Read(cache, "branch:one:history:today", () => Task.FromResult(20));
        oldSource.SetResult(10);

        Assert.Equal(20, fresh);
        Assert.Equal(10, await stale);
        Assert.Equal(20, await Read(cache, "branch:one:history:today", () => Task.FromResult(30)));
    }

    [Fact]
    public async Task ClearPreventsPreviousSessionQueryFromPopulatingCache()
    {
        var cache = new AdminQueryCache();
        var oldSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldRequest = Read(cache, "employee:one:history", () => oldSource.Task);

        cache.Clear();
        Assert.Equal("new account", await Read(cache, "employee:one:history", () => Task.FromResult("new account")));
        oldSource.SetResult("old account");
        Assert.Equal("old account", await oldRequest);
        Assert.Equal("new account", await Read(cache, "employee:one:history", () => Task.FromResult("unexpected")));
    }

    [Fact]
    public async Task SynchronousCompletionAndFailuresDoNotLeaveQueriesInFlight()
    {
        var cache = new AdminQueryCache();
        var calls = 0;
        Assert.Equal(1, await Read(cache, "instant", () => Task.FromResult(++calls)));
        Assert.Equal(1, await Read(cache, "instant", () => Task.FromResult(++calls)));
        Assert.Equal(1, calls);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Read<int>(
            cache, "fails", () => Task.FromException<int>(new InvalidOperationException())));
        Assert.Equal(2, await Read(cache, "fails", () => Task.FromResult(2)));
    }

    private static Task<T> Read<T>(AdminQueryCache cache, string key, Func<Task<T>> factory)
        => cache.GetOrCreateAsync(
            key,
            TimeSpan.FromMinutes(1),
            factory,
            cancellationToken: TestContext.Current.CancellationToken);
}