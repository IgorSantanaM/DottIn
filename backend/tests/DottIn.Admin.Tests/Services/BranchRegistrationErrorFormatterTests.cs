using System.Net;
using DottIn.Admin.Services;

namespace DottIn.Admin.Tests.Services;

public sealed class BranchRegistrationErrorFormatterTests
{
    [Fact]
    public void Format_ReturnsTheApiValidationMessage()
    {
        const string responseBody = """
            {"status":400,"title":"Validation failed","errors":[{"field":"Geolocation","error":"Localização inválida."}]}
            """;

        var result = BranchRegistrationErrorFormatter.Format(HttpStatusCode.BadRequest, responseBody);

        Assert.Equal("Localização inválida.", result);
    }

    [Fact]
    public void Format_ReportsHttpStatusWhenTheResponseIsNotAProblemJson()
    {
        var result = BranchRegistrationErrorFormatter.Format(HttpStatusCode.Forbidden, "<html>Forbidden</html>");

        Assert.Contains("HTTP 403", result, StringComparison.Ordinal);
    }
}
