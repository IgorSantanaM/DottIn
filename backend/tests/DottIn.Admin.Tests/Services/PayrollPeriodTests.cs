using DottIn.Admin.Services;

namespace DottIn.Admin.Tests.Services;

public sealed class PayrollPeriodTests
{
    [Theory]
    [InlineData(2026, 8, 31)]
    [InlineData(2026, 2, 28)]
    [InlineData(2028, 2, 29)]
    [InlineData(2026, 4, 30)]
    [InlineData(2026, 12, 31)]
    [InlineData(9999, 12, 31)]
    public void ExportPeriodIncludesTheEntireSelectedMonth(int year, int month, int days)
    {
        var period = PayrollPeriod.FromMonth(new DateTime(year, month, 15));
        Assert.Equal(new DateOnly(year, month, 1), period.Start);
        Assert.Equal(new DateOnly(year, month, days), period.End);
    }
}
