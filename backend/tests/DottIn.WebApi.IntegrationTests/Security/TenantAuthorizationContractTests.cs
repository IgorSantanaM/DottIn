namespace DottIn.WebApi.IntegrationTests.Security;

public sealed class TenantAuthorizationContractTests
{
    [Fact]
    public void BranchWithoutOwner_AllowsOnlyItsTokenBoundUser()
    {
        var source = ReadSource("src/DottIn.Presentation.WebApi/Security/TenantAccessService.cs");

        Assert.Contains("branch.OwnerId is null && currentUser.BranchId == branchId", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/employee-invitations")]
    [InlineData("/dominio-mappings")]
    [InlineData("/exports/")]
    [InlineData("/timekeeping/branch/")]
    [InlineData("/holiday-calendars/work-records/")]
    public void SensitiveBranchReads_RequireManager(string pathFragment)
    {
        var source = ReadSource("src/DottIn.Presentation.WebApi/Security/TenantAuthorizationFilter.cs");

        Assert.Contains(pathFragment, source, StringComparison.Ordinal);
        Assert.Contains("RequiresManager(path, mutation, isClockAction)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EmployeeDirectoryReads_RequireManager()
    {
        var source = ReadSource("src/DottIn.Presentation.WebApi/Security/TenantAuthorizationFilter.cs");

        Assert.Contains("IsEmployeeDirectoryPath(path)", source, StringComparison.Ordinal);
        Assert.Contains("suffix.Equals(\"/active\"", source, StringComparison.Ordinal);
        Assert.Contains("suffix.StartsWith(\"/cpf/\"", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Commands/AddHoliday/AddHolidayCommandHandler.cs")]
    [InlineData("Commands/ClearHolidays/ClearHolidaysCommandHandler.cs")]
    [InlineData("Commands/RemoveHoliday/RemoveHolidayCommandHandler.cs")]
    [InlineData("Commands/UpdateHoliday/UpdateHolidayCommandHandler.cs")]
    [InlineData("Queries/GetHolidayCalendarById/GetHolidayCalendarByIdQueryHandler.cs")]
    public void CalendarByIdOperationsRequireMatchingBranch(string handlerPath)
    {
        var source = ReadSource($"src/DottIn.Application/Features/HolidayCalendars/{handlerPath}");

        Assert.Contains("holidayCalendar.BranchId != request.BranchId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ComplianceRulesRejectUnownedOrInactiveCalendar()
    {
        var source = ReadSource("src/DottIn.Application/Features/Branches/Commands/SetComplianceRules/SetComplianceRulesCommandHandler.cs");

        Assert.Contains("calendar.BranchId != request.BranchId", source, StringComparison.Ordinal);
        Assert.Contains("!calendar.IsActive", source, StringComparison.Ordinal);
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
