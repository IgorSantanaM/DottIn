namespace DottIn.Admin.Tests.Pages;

public sealed class BranchRegistrationGeolocationContractTests
{
    [Fact]
    public void BranchRegistration_UsesBrowserCoordinatesInsteadOfTheZeroCoordinatePlaceholder()
    {
        var register = ReadSource("clients/DottIn.Admin/Pages/Register.razor");

        Assert.Contains("BrowserGeolocation.GetCurrentPositionAsync", register, StringComparison.Ordinal);
        Assert.DoesNotContain("Geolocation = new { Latitude = 0.0, Longitude = 0.0 }", register, StringComparison.Ordinal);
    }

    [Fact]
    public void BranchRegistration_ExposesOnlyCnpjAsTheCompanyDocumentType()
    {
        var register = ReadSource("clients/DottIn.Admin/Pages/Register.razor");

        Assert.Contains("private string _documentType = \"CNPJ\";", register, StringComparison.Ordinal);
        Assert.DoesNotContain("<MudSelectItem Value=\"@(\"CPF\")\">CPF</MudSelectItem>", register, StringComparison.Ordinal);
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
