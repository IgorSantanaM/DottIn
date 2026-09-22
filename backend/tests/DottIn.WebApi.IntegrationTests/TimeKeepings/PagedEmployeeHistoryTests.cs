using DottIn.Application.Features.TimeKeepings.Queries.GetPagedEmployeeTimeKeeping;
using DottIn.Domain.Core.Exceptions;

namespace DottIn.WebApi.IntegrationTests.TimeKeepings;

public sealed class PagedEmployeeHistoryTests
{
    [Fact]
    public async Task RejectsInvalidPageBeforeQueryingEmployeeData()
    {
        var handler = new GetPagedEmployeeTimeKeepingQueryHandler(null!, null!, null!, null!, null!);
        var query = new GetPagedEmployeeTimeKeepingQuery(
            Guid.NewGuid(), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 22), 0, 25);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(query, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void PersonalEndpointRetainsEmployeeAuthorizationAndDatabasePagination()
    {
        var endpoint = ReadSource("src/DottIn.Presentation.WebApi/Endpoints/TimeKeepingEndpoints.cs");
        var filter = ReadSource("src/DottIn.Presentation.WebApi/Security/TenantAuthorizationFilter.cs");
        var repository = ReadSource("src/DottIn.Infra.Data/Repositories/TimeKeepingRepository.cs");

        Assert.Contains(".AddEndpointFilter<TenantAuthorizationFilter>()", endpoint, StringComparison.Ordinal);
        Assert.Contains("/employee/{employeeId:guid}/history/paged", endpoint, StringComparison.Ordinal);
        Assert.Contains("ReadRouteGuid(http, \"employeeId\")", filter, StringComparison.Ordinal);
        Assert.Contains("CanAccessEmployeeAsync(employeeId.Value", filter, StringComparison.Ordinal);
        Assert.Contains("employeeId != currentUser.EmployeeId && !currentUser.IsAdministrator", filter, StringComparison.Ordinal);
        Assert.Contains("path.Contains(\"/timekeeping/branch/\", StringComparison.OrdinalIgnoreCase)", filter, StringComparison.Ordinal);
        Assert.Contains("tk.EmployeeId == employeeId", repository, StringComparison.Ordinal);
        Assert.Contains("GetPagedByEmployeeAndPeriodAsync", repository, StringComparison.Ordinal);
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
