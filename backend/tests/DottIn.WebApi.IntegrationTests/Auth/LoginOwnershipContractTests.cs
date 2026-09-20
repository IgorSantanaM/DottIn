namespace DottIn.WebApi.IntegrationTests.Auth;

public sealed class LoginOwnershipContractTests
{
    [Fact]
    public void LoginResponse_IdentifiesOwnerByBranchOwnership()
    {
        var source = ReadSource("src/DottIn.Presentation.WebApi/Endpoints/AuthEndpoints.cs");

        Assert.Contains("var isOwner = branch.OwnerId == employee.Id;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("var isOwner = employee.Role == EmployeeRole.Owner;", source, StringComparison.Ordinal);
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
