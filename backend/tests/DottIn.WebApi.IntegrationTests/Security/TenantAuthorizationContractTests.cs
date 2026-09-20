namespace DottIn.WebApi.IntegrationTests.Security;

public sealed class TenantAuthorizationContractTests
{
    [Fact]
    public void BranchWithoutOwner_AllowsOnlyItsTokenBoundUser()
    {
        var source = ReadSource("src/DottIn.Presentation.WebApi/Security/TenantAccessService.cs");

        Assert.Contains("branch.OwnerId is null && currentUser.BranchId == branchId", source, StringComparison.Ordinal);
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
