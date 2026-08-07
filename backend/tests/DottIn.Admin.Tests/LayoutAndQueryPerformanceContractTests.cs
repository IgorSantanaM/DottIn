namespace DottIn.Admin.Tests;

public sealed class LayoutAndQueryPerformanceContractTests
{
    [Fact]
    public void FixedAppBar_ReservesItsHeightInMainContent()
    {
        var layout = ReadSource("clients/DottIn.Admin/Layout/MainLayout.razor");

        Assert.Contains("padding-top: calc(var(--mud-appbar-height) + 24px)", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void OwnerDashboard_LoadsIndependentRequestsConcurrently()
    {
        var dashboard = ReadSource("clients/DottIn.Admin/Pages/Dashboard.razor");

        Assert.Contains("await Task.WhenAll(employeesTask, todayRecordsTask)", dashboard, StringComparison.Ordinal);
    }

    [Fact]
    public void BranchHistoryQuery_DoesNotRunRepositoryQueriesInParallel()
    {
        var handler = ReadSource("src/DottIn.Application/Features/TimeKeepings/Queries/GetBranchTimeKeepingByPeriod/GetBranchTimeKeepingByPeriodQueryHandler.cs");

        Assert.DoesNotContain("Task.WhenAll", handler, StringComparison.Ordinal);
    }

    private static string ReadSource(string relativePath)
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate))
                return File.ReadAllText(candidate);
        }

        throw new FileNotFoundException($"Could not find source file: {relativePath}");
    }
}
