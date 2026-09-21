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
