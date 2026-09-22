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
    public void OwnerDashboard_UsesAggregatedEndpoint()
    {
        var dashboard = ReadSource("clients/DottIn.Admin/Pages/Dashboard.razor");
        var client = ReadSource("clients/DottIn.Admin/Services/AdminApiClient.cs");

        Assert.Contains("GetDashboardSummaryAsync", dashboard, StringComparison.Ordinal);
        Assert.Contains("BranchClock.Apply(summary)", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("SynchronizeBranchClockAsync", dashboard, StringComparison.Ordinal);
        Assert.Contains("/dashboard", client, StringComparison.Ordinal);
    }

    [Fact]
    public void BranchHistoryQuery_DoesNotRunRepositoryQueriesInParallel()
    {
        var handler = ReadSource("src/DottIn.Application/Features/TimeKeepings/Queries/GetBranchTimeKeepingByPeriod/GetBranchTimeKeepingByPeriodQueryHandler.cs");

        Assert.DoesNotContain("Task.WhenAll", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void BranchHistoryPage_UsesDatabasePagination()
    {
        var repository = ReadSource("src/DottIn.Infra.Data/Repositories/TimeKeepingRepository.cs");

        Assert.Contains("CountAsync(token)", repository, StringComparison.Ordinal);
        Assert.Contains(".Skip((pageNumber - 1) * pageSize)", repository, StringComparison.Ordinal);
        Assert.Contains(".Take(pageSize)", repository, StringComparison.Ordinal);
    }

    [Fact]
    public void TimeKeepingPage_UsesPagedEndpoint()
    {
        var page = ReadSource("clients/DottIn.Admin/Pages/TimeKeeping.razor");
        var client = ReadSource("clients/DottIn.Admin/Services/AdminApiClient.cs");

        Assert.Contains("GetPagedBranchHistoryAsync", page, StringComparison.Ordinal);
        Assert.Contains("GetPagedEmployeeHistoryAsync", page, StringComparison.Ordinal);
        Assert.Contains("State.CanViewBranchRecords", page, StringComparison.Ordinal);
        Assert.Contains("/history/paged", client, StringComparison.Ordinal);
        Assert.Contains("/api/timekeeping/employee/{employeeId}/history/paged", client, StringComparison.Ordinal);
        Assert.Contains("PageSize = 25", page, StringComparison.Ordinal);
    }

    [Fact]
    public void HolidayWorkPageAvoidsFullYearAttendanceHistory()
    {
        var page = ReadSource("clients/DottIn.Admin/Pages/Holidays.razor");
        var client = ReadSource("clients/DottIn.Admin/Services/AdminApiClient.cs");

        Assert.Contains("GetHolidayWorkRecordsAsync", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GetBranchHistoryAsync", page, StringComparison.Ordinal);
        Assert.Contains("/holiday-calendars/work-records/", client, StringComparison.Ordinal);
    }
    [Fact]
    public void WebExportsUseStreamDownloadWithoutDynamicScriptEvaluation()
    {
        var index = ReadSource("clients/DottIn.Admin/wwwroot/index.html");
        var timeKeeping = ReadSource("clients/DottIn.Admin/Pages/TimeKeeping.razor");
        var dominio = ReadSource("clients/DottIn.Admin/Pages/ExportDominioDialog.razor");
        var helper = ReadSource("clients/DottIn.Admin/wwwroot/js/download.js");

        Assert.Contains("js/download.js", index, StringComparison.Ordinal);
        Assert.Contains("Download.DownloadAsync", timeKeeping, StringComparison.Ordinal);
        Assert.Contains("Download.DownloadAsync", dominio, StringComparison.Ordinal);
        Assert.DoesNotContain("InvokeVoidAsync(\"eval\"", timeKeeping, StringComparison.Ordinal);
        Assert.DoesNotContain("InvokeVoidAsync(\"eval\"", dominio, StringComparison.Ordinal);
        Assert.Contains("URL.createObjectURL", helper, StringComparison.Ordinal);
        Assert.Contains("URL.revokeObjectURL", helper, StringComparison.Ordinal);
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
