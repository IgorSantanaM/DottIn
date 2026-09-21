using DottIn.Domain.Branches;
using DottIn.Domain.Core.Exceptions;
using DottIn.Domain.TimeKeepings;

namespace DottIn.Domain.Tests.TimeKeepings;

public class TimeKeepingTests
{
    private static readonly Guid BranchId = Guid.NewGuid();
    private static readonly Guid EmployeeId = Guid.NewGuid();
    private static readonly Geolocation Location = new(-20.45, -54.62);

    [Fact]
    public void Constructor_SnapshotsWorkDateTimezoneAndUtcCreationTime()
    {
        var now = new DateTime(2026, 7, 24, 1, 30, 0, DateTimeKind.Utc);
        var workDate = new DateOnly(2026, 7, 23);

        var record = new TimeKeeping(
            BranchId, EmployeeId, Location, workDate,
            "America/Sao_Paulo", now, ClockSource.Mobile);

        Assert.Equal(workDate, record.WorkDate);
        Assert.Equal("America/Sao_Paulo", record.TimeZoneId);
        Assert.Equal(now, record.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, record.CreatedAt.Kind);
    }

    [Fact]
    public void ClockOut_BeforeClockIn_IsRejected()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var record = NewRecord(now);

        Assert.Throws<DomainException>(() => record.ClockOut(now));
    }

    [Fact]
    public void Entries_RejectNonUtcAndOutOfOrderTimestamps()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var record = NewRecord(now);
        record.ClockIn(now);

        Assert.Throws<DomainException>(() => record.StartBreak(now.AddMinutes(-1)));
        Assert.Throws<DomainException>(() => record.StartBreak(DateTime.SpecifyKind(now.AddMinutes(1), DateTimeKind.Unspecified)));
    }

    [Fact]
    public void ClockOut_DuringBreak_ClosesBreakThenJourney()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var record = NewRecord(now);
        record.ClockIn(now);
        record.StartBreak(now.AddHours(4));

        record.ClockOut(now.AddHours(5));

        Assert.Equal(TimeKeepingStatus.Finished, record.Status);
        Assert.Equal(
            new[] { TimeKeepingType.ClockIn, TimeKeepingType.BreakStart, TimeKeepingType.BreakEnd, TimeKeepingType.ClockOut },
            record.Entries.Select(x => x.Type));
    }

    [Fact]
    public void StateTransitions_ChangeConcurrencyToken()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var record = NewRecord(now);
        var initialToken = record.ConcurrencyToken;

        record.ClockIn(now);
        var clockInToken = record.ConcurrencyToken;
        record.StartBreak(now.AddHours(4));

        Assert.NotEqual(Guid.Empty, initialToken);
        Assert.NotEqual(initialToken, clockInToken);
        Assert.NotEqual(clockInToken, record.ConcurrencyToken);
    }

    [Fact]
    public void Entries_PreserveLocationEvidenceForEveryClockAction()
    {
        var now = new DateTime(2026, 7, 24, 12, 0, 0, DateTimeKind.Utc);
        var record = NewRecord(now);

        record.ClockIn(now, Location, 12, now.AddSeconds(-2), ClockSource.Web);
        record.StartBreak(now.AddHours(4), Location, 18, now.AddHours(4).AddSeconds(-2), ClockSource.Mobile);
        record.ClockOut(now.AddHours(5), Location, 20, now.AddHours(5).AddSeconds(-1), ClockSource.Web);

        Assert.All(record.Entries, entry => Assert.NotNull(entry.Location));
        Assert.All(record.Entries, entry => Assert.NotNull(entry.AccuracyMeters));
        Assert.Equal(
            new[] { ClockSource.Web, ClockSource.Mobile, ClockSource.Web, ClockSource.Web },
            record.Entries.Select(entry => entry.Source));
    }

    private static TimeKeeping NewRecord(DateTime now) => new(
        BranchId,
        EmployeeId,
        Location,
        BranchTime.GetLocalDate(now, "America/Sao_Paulo"),
        "America/Sao_Paulo",
        now,
        ClockSource.Mobile);
}
