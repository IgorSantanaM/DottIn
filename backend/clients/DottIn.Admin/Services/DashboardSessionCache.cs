using System.Text.Json;
using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

/// <summary>
/// A short-lived, tab-scoped dashboard snapshot for fast reloads after authentication.
/// </summary>
public sealed class DashboardSessionCache(SessionStorageService storage)
{
    public const string StorageKey = "admin.dashboard.snapshot";
    public static readonly TimeSpan FreshFor = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

    public async Task<DashboardSnapshot?> GetAsync(Guid employeeId, Guid branchId)
    {
        DashboardSnapshot? snapshot;
        try
        {
            snapshot = await storage.GetSessionItemAsync<DashboardSnapshot>(StorageKey);
        }
        catch (JsonException)
        {
            await ClearAsync();
            return null;
        }

        if (snapshot is null)
            return null;

        var age = DateTime.UtcNow - snapshot.SavedAtUtc;
        if (snapshot.EmployeeId != employeeId || snapshot.BranchId != branchId ||
            snapshot.Summary is null || snapshot.Summary.TodayRecords is null ||
            age < TimeSpan.Zero || age > MaxAge ||
            snapshot.Summary.LocalNow.Date != snapshot.Summary.LocalNow.Add(age).Date)
        {
            await ClearAsync();
            return null;
        }

        return snapshot;
    }

    public Task SetAsync(Guid employeeId, Guid branchId, DashboardSummary summary)
        => storage.SetSessionItemAsync(StorageKey,
            new DashboardSnapshot(employeeId, branchId, DateTime.UtcNow, summary));

    public Task ClearAsync() => storage.RemoveItemAsync(StorageKey);
}

public sealed record DashboardSnapshot(
    Guid EmployeeId,
    Guid BranchId,
    DateTime SavedAtUtc,
    DashboardSummary Summary);
