namespace DottIn.WebApi.IntegrationTests.Auth;

public sealed class LoginPerformanceContractTests
{
    [Fact]
    public void PasswordLoginDoesNotLoadUnusedSubscriptionSummary()
    {
        var source = ReadSource("src/DottIn.Presentation.WebApi/Endpoints/AuthEndpoints.cs");
        var loginResponseStart = source.IndexOf(
            "private static async Task<IResult> GenerateLoginResponseAsync",
            StringComparison.Ordinal);
        var loginResponseEnd = source.IndexOf(
            "private static void SetRefreshCookieIfRequested",
            loginResponseStart,
            StringComparison.Ordinal);
        var loginResponse = source[loginResponseStart..loginResponseEnd];

        Assert.DoesNotContain("ITenantSubscriptionService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetByOwnerIdAsync", loginResponse, StringComparison.Ordinal);
        Assert.Contains("Subscription: null", loginResponse, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticationIsWarmedBeforeTheApiStartsListening()
    {
        var program = ReadSource("src/DottIn.Presentation.WebApi/Program.cs");
        var warmup = ReadSource("src/DottIn.Presentation.WebApi/Performance/AuthenticationWarmup.cs");

        Assert.Contains("await AuthenticationWarmup.TryWarmAsync", program, StringComparison.Ordinal);
        Assert.Contains("EnhancedVerify", warmup, StringComparison.Ordinal);
        Assert.Contains("db.Employees", warmup, StringComparison.Ordinal);
        Assert.Contains("db.Branches", warmup, StringComparison.Ordinal);
    }

    [Fact]
    public void OwnerEmployeeCountUsesOneDatabaseQuery()
    {
        var repository = ReadSource("src/DottIn.Infra.Data/Repositories/EmployeeRepository.cs");
        var start = repository.IndexOf("CountActiveByOwnerIdAsync", StringComparison.Ordinal);
        var end = repository.IndexOf("AddEmployeeImageAsync", start, StringComparison.Ordinal);
        var method = repository[start..end];

        Assert.Contains("context.Branches.Any", method, StringComparison.Ordinal);
        Assert.DoesNotContain("ToListAsync", method, StringComparison.Ordinal);
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
