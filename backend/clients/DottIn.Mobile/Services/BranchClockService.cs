using DottIn.Mobile.Services.Interfaces;

namespace DottIn.Mobile.Services;

/// <summary>
/// Keeps the mobile UI aligned with the server time in the configured branch timezone.
/// </summary>
public sealed class BranchClockService(IBranchApi branchApi)
{
    private DateTime _utcAtSynchronization = DateTime.UtcNow;
    private DateTime _branchTimeAtSynchronization = DateTime.UtcNow;

    public string TimeZoneId { get; private set; } = "UTC";

    public DateTime Now
        => _branchTimeAtSynchronization + (DateTime.UtcNow - _utcAtSynchronization);

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task SynchronizeAsync(Guid branchId)
    {
        var clock = await branchApi.GetClockAsync(branchId);
        _utcAtSynchronization = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        _branchTimeAtSynchronization = DateTime.SpecifyKind(clock.LocalNow, DateTimeKind.Unspecified);
        TimeZoneId = clock.TimeZoneId;
    }
}
