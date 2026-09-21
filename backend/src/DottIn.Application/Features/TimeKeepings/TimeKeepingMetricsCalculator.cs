using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;

namespace DottIn.Application.Features.TimeKeepings;

public sealed record TimeKeepingMetrics(
    DateTime? ClockInUtc,
    DateTime? ClockOutUtc,
    TimeSpan TotalWorked,
    TimeSpan TotalBreak,
    TimeSpan NocturnalWorked,
    TimeKeepingStatus Status);

public sealed record ScheduleMetrics(
    TimeSpan ExpectedWorked,
    TimeSpan Late,
    TimeSpan EarlyDeparture,
    TimeSpan Overtime);

public static class TimeKeepingMetricsCalculator
{
    private static readonly TimeOnly NightStart = new(22, 0);
    private static readonly TimeOnly NightEnd = new(6, 0);

    public static TimeKeepingMetrics Calculate(
        TimeKeeping timeKeeping,
        DateTime nowUtc,
        IEnumerable<TimeKeepingAdjustment>? adjustments = null)
    {
        BranchTime.NormalizeUtc(nowUtc);

        var entries = BuildEffectiveEntries(timeKeeping, adjustments);
        var clockIn = entries.FirstOrDefault(entry => entry.Type == TimeKeepingType.ClockIn)?.Timestamp;
        var clockOut = entries.LastOrDefault(entry => entry.Type == TimeKeepingType.ClockOut)?.Timestamp;
        var effectiveNow = clockIn.HasValue && nowUtc < clockIn.Value ? clockIn.Value : nowUtc;

        var worked = TimeSpan.Zero;
        var breaks = TimeSpan.Zero;
        var nocturnal = TimeSpan.Zero;
        DateTime? activeWorkStart = null;
        DateTime? activeBreakStart = null;

        foreach (var entry in entries)
        {
            switch (entry.Type)
            {
                case TimeKeepingType.ClockIn:
                case TimeKeepingType.BreakEnd:
                    if (entry.Type == TimeKeepingType.BreakEnd && activeBreakStart.HasValue)
                    {
                        breaks += PositiveDuration(activeBreakStart.Value, entry.Timestamp);
                        activeBreakStart = null;
                    }
                    activeWorkStart = entry.Timestamp;
                    break;

                case TimeKeepingType.BreakStart:
                    if (activeWorkStart.HasValue)
                    {
                        AddWorkInterval(activeWorkStart.Value, entry.Timestamp, timeKeeping.TimeZoneId, ref worked, ref nocturnal);
                        activeWorkStart = null;
                    }
                    activeBreakStart = entry.Timestamp;
                    break;

                case TimeKeepingType.ClockOut:
                    if (activeBreakStart.HasValue)
                    {
                        breaks += PositiveDuration(activeBreakStart.Value, entry.Timestamp);
                        activeBreakStart = null;
                    }
                    if (activeWorkStart.HasValue)
                    {
                        AddWorkInterval(activeWorkStart.Value, entry.Timestamp, timeKeeping.TimeZoneId, ref worked, ref nocturnal);
                        activeWorkStart = null;
                    }
                    break;
            }
        }

        if (!clockOut.HasValue)
        {
            if (activeBreakStart.HasValue)
                breaks += PositiveDuration(activeBreakStart.Value, effectiveNow);
            if (activeWorkStart.HasValue)
                AddWorkInterval(activeWorkStart.Value, effectiveNow, timeKeeping.TimeZoneId, ref worked, ref nocturnal);
        }

        return new TimeKeepingMetrics(clockIn, clockOut, worked, breaks, nocturnal, GetStatus(entries));
    }

    public static IReadOnlyList<TimeEntry> BuildEffectiveEntries(
        TimeKeeping timeKeeping,
        IEnumerable<TimeKeepingAdjustment>? adjustments)
    {
        var entries = timeKeeping.Entries
            .Select(entry => new TimeEntry(
                entry.Timestamp, entry.Type, entry.Location, entry.AccuracyMeters,
                entry.CapturedAtUtc, entry.Source))
            .ToList();

        IEnumerable<TimeKeepingAdjustment> approvedAdjustments = adjustments?
            .Where(x => x.Status == TimeKeepingAdjustmentStatus.Approved)
            .OrderBy(x => x.CreatedAt) ?? Enumerable.Empty<TimeKeepingAdjustment>();

        foreach (var adjustment in approvedAdjustments)
        {
            if (adjustment.OriginalTimestamp.HasValue)
            {
                var index = entries.FindIndex(entry =>
                    entry.Type == adjustment.EntryType &&
                    entry.Timestamp == adjustment.OriginalTimestamp.Value);
                if (index >= 0)
                    entries.RemoveAt(index);
            }

            entries.Add(new TimeEntry(
                adjustment.ProposedTimestamp, adjustment.EntryType, source: ClockSource.Web));
        }

        return entries.OrderBy(entry => entry.Timestamp).ToList();
    }

    public static bool HasValidSequence(
        TimeKeeping timeKeeping,
        IEnumerable<TimeKeepingAdjustment>? adjustments)
    {
        var expected = TimeKeepingStatus.NotStarted;
        foreach (var entry in BuildEffectiveEntries(timeKeeping, adjustments))
        {
            expected = (expected, entry.Type) switch
            {
                (TimeKeepingStatus.NotStarted, TimeKeepingType.ClockIn) => TimeKeepingStatus.Working,
                (TimeKeepingStatus.Working, TimeKeepingType.BreakStart) => TimeKeepingStatus.OnBreak,
                (TimeKeepingStatus.OnBreak, TimeKeepingType.BreakEnd) => TimeKeepingStatus.Working,
                (TimeKeepingStatus.Working, TimeKeepingType.ClockOut) => TimeKeepingStatus.Finished,
                _ => (TimeKeepingStatus)(-1)
            };

            if ((int)expected == -1)
                return false;
        }

        return expected != TimeKeepingStatus.NotStarted;
    }

    public static ScheduleMetrics CalculateSchedule(
        TimeKeeping timeKeeping,
        Employee employee,
        int toleranceMinutes,
        TimeKeepingMetrics metrics)
    {
        var shiftStart = timeKeeping.WorkDate.ToDateTime(employee.StartWorkTime);
        var shiftEndDate = employee.AllowOvernightShifts ? timeKeeping.WorkDate.AddDays(1) : timeKeeping.WorkDate;
        var shiftEnd = shiftEndDate.ToDateTime(employee.EndWorkTime);
        var expectedWorked = PositiveDuration(shiftStart, shiftEnd) - Duration(employee.IntervalStart, employee.IntervalEnd);
        if (expectedWorked < TimeSpan.Zero)
            expectedWorked = TimeSpan.Zero;

        var clockInLocal = metrics.ClockInUtc.HasValue
            ? BranchTime.ToLocal(metrics.ClockInUtc.Value, timeKeeping.TimeZoneId)
            : (DateTime?)null;
        var clockOutLocal = metrics.ClockOutUtc.HasValue
            ? BranchTime.ToLocal(metrics.ClockOutUtc.Value, timeKeeping.TimeZoneId)
            : (DateTime?)null;
        var tolerance = TimeSpan.FromMinutes(Math.Max(0, toleranceMinutes));

        return new ScheduleMetrics(
            expectedWorked,
            ApplyTolerance(clockInLocal.HasValue ? clockInLocal.Value - shiftStart : TimeSpan.Zero, tolerance),
            ApplyTolerance(clockOutLocal.HasValue ? shiftEnd - clockOutLocal.Value : TimeSpan.Zero, tolerance),
            ApplyTolerance(metrics.TotalWorked - expectedWorked, tolerance));
    }

    private static void AddWorkInterval(DateTime startUtc, DateTime endUtc, string timeZoneId, ref TimeSpan worked, ref TimeSpan nocturnal)
    {
        var duration = PositiveDuration(startUtc, endUtc);
        if (duration == TimeSpan.Zero)
            return;

        worked += duration;
        nocturnal += CalculateNocturnal(startUtc, endUtc, timeZoneId);
    }

    private static TimeSpan CalculateNocturnal(DateTime startUtc, DateTime endUtc, string timeZoneId)
    {
        var localStart = BranchTime.ToLocal(startUtc, timeZoneId);
        var localEnd = BranchTime.ToLocal(endUtc, timeZoneId);
        var total = TimeSpan.Zero;

        for (var date = localStart.Date.AddDays(-1); date <= localEnd.Date; date = date.AddDays(1))
        {
            var windowStart = date.Add(NightStart.ToTimeSpan());
            var windowEnd = date.AddDays(1).Add(NightEnd.ToTimeSpan());
            var overlapStart = localStart > windowStart ? localStart : windowStart;
            var overlapEnd = localEnd < windowEnd ? localEnd : windowEnd;
            if (overlapEnd > overlapStart)
                total += overlapEnd - overlapStart;
        }

        return total;
    }

    private static TimeSpan Duration(TimeOnly start, TimeOnly end)
        => start <= end ? end - start : TimeSpan.FromHours(24) - start.ToTimeSpan() + end.ToTimeSpan();

    private static TimeSpan PositiveDuration(DateTime start, DateTime end)
        => end > start ? end - start : TimeSpan.Zero;

    private static TimeSpan ApplyTolerance(TimeSpan candidate, TimeSpan tolerance)
        => candidate > tolerance ? candidate : TimeSpan.Zero;

    private static TimeKeepingStatus GetStatus(IReadOnlyList<TimeEntry> entries)
        => entries.LastOrDefault()?.Type switch
        {
            TimeKeepingType.ClockIn => TimeKeepingStatus.Working,
            TimeKeepingType.BreakStart => TimeKeepingStatus.OnBreak,
            TimeKeepingType.BreakEnd => TimeKeepingStatus.Working,
            TimeKeepingType.ClockOut => TimeKeepingStatus.Finished,
            _ => TimeKeepingStatus.NotStarted
        };
}
