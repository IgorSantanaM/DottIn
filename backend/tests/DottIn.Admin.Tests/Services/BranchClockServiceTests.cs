using DottIn.Admin.Models;
using DottIn.Admin.Services;

namespace DottIn.Admin.Tests.Services;

public sealed class BranchClockServiceTests
{
    [Fact]
    public void DashboardSummaryAnchorsTheTickingBranchClock()
    {
        var api = new AdminApiClient(new HttpClient(), new AdminQueryCache());
        var clock = new BranchClockService(api);
        var utcNow = DateTime.UtcNow;
        var localNow = DateTime.SpecifyKind(utcNow.AddHours(-4), DateTimeKind.Unspecified);
        var summary = new DashboardSummary(0, [], null, utcNow, localNow, "America/Cuiaba");

        clock.Apply(summary);

        Assert.Equal("America/Cuiaba", clock.TimeZoneId);
        Assert.InRange((clock.Now - localNow).TotalSeconds, 0, 2);
        Assert.Equal(DateOnly.FromDateTime(localNow), clock.Today);
    }
}