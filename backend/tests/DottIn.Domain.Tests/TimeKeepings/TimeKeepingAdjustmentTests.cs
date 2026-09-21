using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.TimeKeepings;

namespace DottIn.Domain.Tests.TimeKeepings;

public sealed class TimeKeepingAdjustmentTests
{
    [Fact]
    public void Adjustment_PreservesAuditAndCanBeApprovedOnce()
    {
        var requester = Guid.NewGuid();
        var reviewer = Guid.NewGuid();
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        var adjustment = NewAdjustment(requester, now);

        adjustment.Approve(reviewer, now.AddMinutes(10), "Conferido com o gestor.");

        Assert.Equal(TimeKeepingAdjustmentStatus.Approved, adjustment.Status);
        Assert.Equal(reviewer, adjustment.ReviewedByEmployeeId);
        Assert.Equal(now.AddMinutes(10), adjustment.ReviewedAt);
        Assert.Throws<DomainException>(() => adjustment.Reject(reviewer, now.AddMinutes(11)));
    }

    [Fact]
    public void Requester_CannotApproveOwnAdjustment()
    {
        var requester = Guid.NewGuid();
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        var adjustment = NewAdjustment(requester, now);

        Assert.Throws<DomainException>(() => adjustment.Approve(requester, now.AddMinutes(1)));
    }

    private static TimeKeepingAdjustment NewAdjustment(Guid requester, DateTime now)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            requester,
            TimeKeepingType.ClockOut,
            null,
            now.AddHours(8),
            "Esqueci de registrar a saída.",
            now);
}
