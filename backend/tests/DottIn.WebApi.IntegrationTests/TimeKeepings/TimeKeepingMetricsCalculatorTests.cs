using DottIn.Application.Features.TimeKeepings;
using DottIn.Domain.Branches;
using DottIn.Domain.Employees;
using DottIn.Domain.TimeKeepings;
using DottIn.Domain.ValueObjects;

namespace DottIn.WebApi.IntegrationTests.TimeKeepings;

public sealed class TimeKeepingMetricsCalculatorTests
{
    [Fact]
    public void Calculate_ExcludesBreakAndCountsOnlyNightOverlap()
    {
        var start = new DateTime(2026, 9, 20, 23, 0, 0, DateTimeKind.Utc);
        var record = NewRecord(start, "UTC");
        record.ClockIn(start);
        record.StartBreak(start.AddHours(2));
        record.EndBreak(start.AddHours(2.5));
        record.ClockOut(start.AddHours(8));

        var metrics = TimeKeepingMetricsCalculator.Calculate(record, start.AddHours(8));

        Assert.Equal(TimeSpan.FromHours(7.5), metrics.TotalWorked);
        Assert.Equal(TimeSpan.FromMinutes(30), metrics.TotalBreak);
        Assert.Equal(TimeSpan.FromHours(6.5), metrics.NocturnalWorked);
    }

    [Fact]
    public void CalculateSchedule_AppliesConfiguredTolerance()
    {
        var start = new DateTime(2026, 9, 21, 11, 11, 0, DateTimeKind.Utc);
        var record = NewRecord(start, "America/Cuiaba");
        record.ClockIn(start);
        record.ClockOut(start.AddHours(8));
        var employee = new Employee(
            "Funcionário Teste",
            new Document("52998224725"),
            record.BranchId,
            new TimeOnly(7, 0),
            new TimeOnly(16, 0),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0));

        var metrics = TimeKeepingMetricsCalculator.Calculate(record, start.AddHours(8));
        var schedule = TimeKeepingMetricsCalculator.CalculateSchedule(record, employee, 10, metrics);

        Assert.Equal(TimeSpan.FromMinutes(11), schedule.Late);
        Assert.Equal(TimeSpan.Zero, schedule.Overtime);
        Assert.Equal(TimeSpan.FromHours(8), schedule.ExpectedWorked);
    }

    private static TimeKeeping NewRecord(DateTime start, string timeZoneId)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Geolocation(-15.6, -56.1),
            BranchTime.GetLocalDate(start, timeZoneId),
            timeZoneId,
            start,
            ClockSource.Mobile);
}
