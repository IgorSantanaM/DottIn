using System.Data.Common;
using DottIn.Presentation.WebApi.Health;

namespace DottIn.WebApi.IntegrationTests.Health;

public sealed class DatabaseReadinessProbeTests
{
    [Fact]
    public async Task ReturnsReadyWhenDatabaseConnects()
    {
        var ready = await DatabaseReadinessProbe.IsReadyAsync(
            _ => Task.FromResult(true),
            TestContext.Current.CancellationToken);

        Assert.True(ready);
    }

    [Fact]
    public async Task ReturnsUnavailableWhenDependencyTimesOut()
    {
        var ready = await DatabaseReadinessProbe.IsReadyAsync(
            async token =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return true;
            },
            TestContext.Current.CancellationToken,
            TimeSpan.FromMilliseconds(20));

        Assert.False(ready);
    }

    [Fact]
    public async Task ReturnsUnavailableForDatabaseError()
    {
        var ready = await DatabaseReadinessProbe.IsReadyAsync(
            _ => throw new TestDbException(),
            TestContext.Current.CancellationToken);

        Assert.False(ready);
    }

    [Fact]
    public async Task DoesNotHideClientCancellation()
    {
        using var canceled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            DatabaseReadinessProbe.IsReadyAsync(
                async token =>
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return true;
                },
                canceled.Token));
    }

    private sealed class TestDbException : DbException;
}
