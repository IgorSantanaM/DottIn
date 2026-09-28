namespace DottIn.WebApi.IntegrationTests.TimeKeepings;

public sealed class ClockSourceSecurityContractTests
{
    [Fact]
    public void EmployeeMayReportWebButCannotReportKioskSource()
    {
        var source = ReadSource("src/DottIn.Presentation.WebApi/Endpoints/TimeKeepingEndpoints.cs");
        var start = source.IndexOf("private static ClockSource ResolveClockSource", StringComparison.Ordinal);
        var end = source.IndexOf("#endregion", start, StringComparison.Ordinal);
        var method = source[start..end];

        Assert.Contains("requestedSource == ClockSource.Web", method, StringComparison.Ordinal);
        Assert.Contains("? ClockSource.Web", method, StringComparison.Ordinal);
        Assert.Contains(": ClockSource.Mobile", method, StringComparison.Ordinal);
        Assert.Contains("return ClockSource.Kiosk", method, StringComparison.Ordinal);
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
