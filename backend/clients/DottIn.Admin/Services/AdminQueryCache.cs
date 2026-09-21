namespace DottIn.Admin.Services;

public sealed class AdminQueryCache
{
    private readonly object sync = new();
    private readonly Dictionary<string, CacheEntry> entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Task<object?>> inFlight = new(StringComparer.Ordinal);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan lifetime,
        Func<Task<T>> factory,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        Task<object?> pending;
        lock (sync)
        {
            if (!forceRefresh && entries.TryGetValue(key, out var cached) && cached.ExpiresAtUtc > DateTime.UtcNow)
                return (T)cached.Value;

            if (!inFlight.TryGetValue(key, out pending!))
            {
                pending = ExecuteAsync(key, lifetime, factory);
                inFlight[key] = pending;
            }
        }

        return await Await<T>(pending, cancellationToken);
    }

    public void Invalidate(string keyPrefix)
    {
        lock (sync)
        {
            foreach (var key in entries.Keys.Where(key => key.StartsWith(keyPrefix, StringComparison.Ordinal)).ToArray())
                entries.Remove(key);
        }
    }

    public void Clear()
    {
        lock (sync)
            entries.Clear();
    }

    private async Task<object?> ExecuteAsync<T>(string key, TimeSpan lifetime, Func<Task<T>> factory)
    {
        try
        {
            var value = await factory();
            lock (sync)
                entries[key] = new CacheEntry(value!, DateTime.UtcNow.Add(lifetime));
            return value;
        }
        finally
        {
            lock (sync)
                inFlight.Remove(key);
        }
    }

    private static async Task<T> Await<T>(Task<object?> pending, CancellationToken cancellationToken)
    {
        var value = await pending.WaitAsync(cancellationToken);
        return (T)value!;
    }

    private sealed record CacheEntry(object Value, DateTime ExpiresAtUtc);
}