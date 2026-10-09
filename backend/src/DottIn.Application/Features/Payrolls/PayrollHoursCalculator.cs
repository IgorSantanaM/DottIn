using DottIn.Application.Features.TimeKeepings;
using DottIn.Domain.TimeKeepings;

namespace DottIn.Application.Features.Payrolls;

public sealed record PayrollHours(long WorkedMinutes, bool HasIncompleteRecords);

public static class PayrollHoursCalculator
{
    public static IReadOnlyDictionary<Guid, PayrollHours> Calculate(
        Guid branchId, int year, int month,
        IEnumerable<TimeKeeping> records,
        IEnumerable<TimeKeepingAdjustment> approvedAdjustments)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var adjustments = approvedAdjustments
            .Where(x => x.Status == TimeKeepingAdjustmentStatus.Approved)
            .GroupBy(x => x.TimeKeepingId)
            .ToDictionary(x => x.Key, x => x.AsEnumerable());

        return records
            .Where(x => x.BranchId == branchId && x.WorkDate >= first && x.WorkDate <= last)
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(group => group.Key, group =>
            {
                long workedTicks = 0;
                var incomplete = false;
                foreach (var record in group)
                {
                    adjustments.TryGetValue(record.Id, out var changes);
                    var valid = TimeKeepingMetricsCalculator.HasValidSequence(record, changes);
                    var metrics = TimeKeepingMetricsCalculator.Calculate(record, DateTime.UtcNow, changes);
                    if (!valid || metrics.Status != TimeKeepingStatus.Finished)
                    {
                        incomplete = true;
                        continue;
                    }
                    workedTicks = checked(workedTicks + metrics.TotalWorked.Ticks);
                }
                return new PayrollHours(workedTicks / TimeSpan.TicksPerMinute, incomplete);
            });
    }
}
