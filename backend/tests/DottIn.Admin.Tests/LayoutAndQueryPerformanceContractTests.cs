namespace DottIn.Admin.Tests;

public sealed class LayoutAndQueryPerformanceContractTests
{
    [Fact]
    public void FixedAppBar_ReservesItsHeightInMainContent()
    {
        var layout = ReadSource("clients/DottIn.Admin/Layout/MainLayout.razor");

        var css = ReadSource("clients/DottIn.Admin/wwwroot/css/app.css");
        Assert.Contains("Class=\"dottin-main-content\"", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Class=\"pa-6 dottin-main-content\"", layout, StringComparison.Ordinal);
        Assert.Contains("padding: 72px 24px 24px", css, StringComparison.Ordinal);
        Assert.Contains("padding: 76px max(16px", css, StringComparison.Ordinal);
    }

    [Fact]
    public void PhoneNavigationIsTemporaryInitiallyClosedAndClosesAfterNavigation()
    {
        var layout = ReadSource("clients/DottIn.Admin/Layout/MainLayout.razor");
        Assert.Contains("Variant=\"DrawerVariant.Temporary\"", layout);
        Assert.Contains("Variant=\"DrawerVariant.Mini\"", layout);
        Assert.Contains("private bool _mobileDrawerOpen;", layout);
        Assert.Equal(2, layout.Split("<OperationalNavMenu CanViewEmployees=\"@State.CanViewEmployees\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("Navigation.LocationChanged += OnLocationChanged", layout);
        Assert.Contains("Navigation.LocationChanged -= OnLocationChanged", layout);
        Assert.Contains("_mobileDrawerOpen = false", layout);
        var css = ReadSource("clients/DottIn.Admin/wwwroot/css/app.css");
        Assert.Contains(".mud-drawer.dottin-desktop-navigation { display: none; }", css);
        Assert.Contains(".mud-drawer.dottin-mobile-navigation { display: flex; }", css);
    }

    [Theory]
    [InlineData("Employees", "dottin-employee-filters")]
    [InlineData("Holidays", "dottin-calendar-header")]
    [InlineData("Dashboard", "dottin-dashboard-clock")]
    public void OperationalPagesUseResponsiveLayoutContracts(string page, string cssClass)
    {
        Assert.Contains(cssClass, ReadSource($"clients/DottIn.Admin/Pages/{page}.razor"));
        Assert.Contains($".{cssClass}", ReadSource("clients/DottIn.Admin/wwwroot/css/app.css"));
    }

    [Fact]
    public void MobileLayoutWrapsContentInsteadOfHidingBodyOverflow()
    {
        var css = ReadSource("clients/DottIn.Admin/wwwroot/css/app.css");
        Assert.Contains("grid-template-columns: repeat(2, minmax(0, 1fr))", css);
        Assert.Contains("font-size: clamp(", css);
        Assert.Contains("overflow-wrap: anywhere", css);
        Assert.Contains("env(safe-area-inset-bottom)", css);
        Assert.DoesNotContain("overflow-x: hidden", css);
        Assert.Contains("viewport-fit=cover", ReadSource("clients/DottIn.Admin/wwwroot/index.html"));
    }

    [Fact]
    public void UserMenuAndAppBarShowAuthenticatedRole()
    {
        var layout = ReadSource("clients/DottIn.Admin/Layout/MainLayout.razor");

        Assert.Equal(2, layout.Split("@State.RoleLabel", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain(">Administrador</MudText>", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void EmployeeDirectoryIsProtectedInNavigationAndBeforePageRendering()
    {
        var menu = ReadSource("clients/DottIn.Admin/Layout/OperationalNavMenu.razor");
        var page = ReadSource("clients/DottIn.Admin/Pages/Employees.razor");
        var route = ReadSource("clients/DottIn.Admin/Routing/OperationalRouteView.razor");
        Assert.Contains("@if (CanViewEmployees)", menu);
        Assert.Contains("[Parameter] public bool CanViewEmployees", menu);
        Assert.Contains("@attribute [EmployeeDirectoryAccessRequired]", page);
        Assert.Contains("(!RequiresEmployeeDirectoryAccess || State.CanViewEmployees)", route);
        Assert.Contains("RequiresEmployeeDirectoryAccess && !State.CanViewEmployees", route);
        Assert.Contains("return \"/dashboard\"", route);
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
    [Fact]
    public void OwnerNavigationIsReactiveAndPayrollExportIsAvailableWithoutCurrentPeriodRecords()
    {
        var layout = ReadSource("clients/DottIn.Admin/Layout/MainLayout.razor");
        var menu = ReadSource("clients/DottIn.Admin/Layout/OperationalNavMenu.razor");
        var records = ReadSource("clients/DottIn.Admin/Pages/TimeKeeping.razor");
        Assert.Equal(2, layout.Split("IsOwner=\"@State.IsOwner\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("[Parameter] public bool IsOwner", menu);
        Assert.Contains("@if (IsOwner)", menu);
        Assert.Contains("Href=\"/billing\"", menu);
        Assert.Contains("@if (State.IsOwner)", records);
        Assert.DoesNotContain("State.IsOwner && _records.Any()", records);
        Assert.Contains("OnClick=\"ExportDominio\"", records);
    }

    [Fact]
    public void BranchCreationRequiresOwnerAndUsesReviewBeforeSaving()
    {
        var page = ReadSource("clients/DottIn.Admin/Pages/Branches.razor");
        var dialog = ReadSource("clients/DottIn.Admin/Pages/CreateBranchDialog.razor");
        var route = ReadSource("clients/DottIn.Admin/Routing/OperationalRouteView.razor");
        Assert.Contains("@attribute [OwnerAccessRequired]", page);
        Assert.Contains("(!RequiresOwnerAccess || State.IsOwner)", route);
        Assert.Contains("RequiresOwnerAccess && !State.IsOwner", route);
        Assert.Contains("Subscription?.CanAddBranch != true", page);
        Assert.Contains("GetBranchManagementAsync(forceRefresh: true)", page);
        Assert.Contains("State.SelectBranchAsync(allowed)", page);
        Assert.Contains("Copiar configurações da matriz", dialog);
        Assert.Contains("if (_saving || !_reviewing) return;", dialog);
        Assert.Contains("_timezone = Headquarters.TimeZoneId", dialog);
        Assert.Contains("_radius = Headquarters.AllowedRadiusMeters", dialog);
        Assert.Contains("xs=\"12\" sm=\"6\"", dialog);
        Assert.DoesNotContain("CreateCheckout", dialog);
    }

    [Fact]
    public void PayrollHasDedicatedOwnerNavigationRouteAndFunctionalExportActions()
    {
        var menu = ReadSource("clients/DottIn.Admin/Layout/OperationalNavMenu.razor");
        var page = ReadSource("clients/DottIn.Admin/Pages/Payroll.razor");
        var ownerSection = menu[menu.IndexOf("@if (IsOwner)", StringComparison.Ordinal)..];
        Assert.Contains("Href=\"/payroll\"", ownerSection);
        Assert.Contains(">Folha de pagamento</MudNavLink>", ownerSection);
        Assert.Contains("@page \"/payroll\"", page);
        Assert.Contains("@attribute [OwnerAccessRequired]", page);
        Assert.Contains("@attribute [OperationalAccessRequired]", page);
        Assert.Contains("ShowAsync<DominioMappingDialog>", page);
        Assert.Contains("ShowAsync<ExportDominioDialog>", page);
        Assert.Contains("d => d.ReferenceMonth", page);
        Assert.Contains("Api.ExportCsvAsync(State.BranchId, period.Start, period.End)", page);
        Assert.Contains("Clock.SynchronizeAsync(State.BranchId)", page);
        Assert.DoesNotContain("_records.Any()", page);
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
