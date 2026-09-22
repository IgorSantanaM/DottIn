using System.Data.Common;

namespace DottIn.Presentation.WebApi.Health;

public static class DatabaseReadinessProbe
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    public static async Task<bool> IsReadyAsync(
        Func<CancellationToken, Task<bool>> canConnect,
        CancellationToken requestAborted,
        TimeSpan? timeout = null)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(requestAborted);
        deadline.CancelAfter(timeout ?? DefaultTimeout);

        try
        {
            return await canConnect(deadline.Token);
        }
        catch (OperationCanceledException) when (!requestAborted.IsCancellationRequested)
        {
            return false;
        }
        catch (DbException)
        {
            return false;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
