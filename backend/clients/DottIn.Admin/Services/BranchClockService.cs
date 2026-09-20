using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

/// <summary>
/// Keeps a locally ticking clock anchored to the server time in the branch timezone.
/// </summary>
public sealed class BranchClockService(AdminApiClient api)
{
    private DateTime _utcAtSynchronization = DateTime.UtcNow;
    private DateTime _branchTimeAtSynchronization = DateTime.UtcNow;

    public string TimeZoneId { get; private set; } = "UTC";

    public DateTime Now
        => _branchTimeAtSynchronization + (DateTime.UtcNow - _utcAtSynchronization);

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public async Task SynchronizeAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var clock = await api.GetBranchClockAsync(branchId, cancellationToken);
        Apply(clock);
    }

    private void Apply(BranchClockResponse clock)
    {
        _utcAtSynchronization = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        _branchTimeAtSynchronization = DateTime.SpecifyKind(clock.LocalNow, DateTimeKind.Unspecified);
        TimeZoneId = clock.TimeZoneId;
    }
}
