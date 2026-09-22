namespace DottIn.Admin.Services;

public sealed class AdminQueryCache
{
    private readonly object sync = new();
    private readonly Dictionary<string, CacheEntry> entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PendingQuery> inFlight = new(StringComparer.Ordinal);

    public Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<Task<T>> factory,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        PendingQuery pending;
        var startQuery = false;
        lock (sync)
        {
            if (!forceRefresh && entries.TryGetValue(key, out var cached) && cached.ExpiresAtUtc > DateTime.UtcNow)
                return Task.FromResult((T)cached.Value!);

            if (!inFlight.TryGetValue(key, out pending!))
            {
                pending = new PendingQuery();
                inFlight[key] = pending;
                startQuery = true;
            }
        }

        if (startQuery)
            _ = ExecuteAsync(key, lifetime, factory, pending);

        return Await<T>(pending.Completion.Task, cancellationToken);
    }

    public void Invalidate(string keyPrefix)
    {
        lock (sync)
        {
            foreach (var key in entries.Keys.Where(key => key.StartsWith(keyPrefix, StringComparison.Ordinal)).ToArray())
                entries.Remove(key);

            foreach (var key in inFlight.Keys.Where(key => key.StartsWith(keyPrefix, StringComparison.Ordinal)).ToArray())
            {
                inFlight[key].Invalidated = true;
                inFlight.Remove(key);
            }
        }
    }

    public void Clear()
    {
        lock (sync)
        {
            entries.Clear();
            foreach (var pending in inFlight.Values)
                pending.Invalidated = true;
            inFlight.Clear();
        }
    }

    private async Task ExecuteAsync<T>(string key, TimeSpan lifetime, Func<Task<T>> factory, PendingQuery pending)
    {
        try
        {
            var value = await factory();
            lock (sync)
            {
                if (!pending.Invalidated)
                    entries[key] = new CacheEntry(value, DateTime.UtcNow.Add(lifetime));
                if (inFlight.TryGetValue(key, out var current) && ReferenceEquals(current, pending))
                    inFlight.Remove(key);
            }
            pending.Completion.TrySetResult(value);
        }
        catch (Exception exception)
        {
            lock (sync)
            {
                if (inFlight.TryGetValue(key, out var current) && ReferenceEquals(current, pending))
                    inFlight.Remove(key);
            }
            pending.Completion.TrySetException(exception);
        }
    }

    private static async Task<T> Await<T>(Task<object?> pending, CancellationToken cancellationToken)
        => (T)(await pending.WaitAsync(cancellationToken))!;

    private sealed record CacheEntry(object? Value, DateTime ExpiresAtUtc);

    private sealed class PendingQuery
    {
        public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Invalidated { get; set; }
    }
}