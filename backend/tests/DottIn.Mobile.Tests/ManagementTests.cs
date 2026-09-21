using System.Net;
using System.Text;
using DottIn.Mobile.Services;

namespace DottIn.Mobile.Tests;

public class ManagementTests
{
    [Theory]
    [InlineData("1", 10, true)]
    [InlineData("0000150", 10, true)]
    [InlineData("0", 10, false)]
    [InlineData("000", 10, false)]
    [InlineData("12a", 10, false)]
    [InlineData("1.2", 10, false)]
    [InlineData("12345", 4, false)]
    [InlineData("", 10, false)]
    public void Payroll_codes_match_numeric_limits(string value, int size, bool expected)
        => Assert.Equal(expected, ManagementRules.NumericCode(value, size));

    [Fact]
    public void Report_hours_do_not_wrap_at_24()
        => Assert.Equal("150:30", ManagementRules.Hours(TimeSpan.FromMinutes(9030)));

    [Fact]
    public void Invalid_report_periods_are_rejected()
    {
        var date = new DateTime(2026, 9, 1);
        Assert.True(ManagementRules.ValidPeriod(date, date));
        Assert.False(ManagementRules.ValidPeriod(date.AddDays(1), date));
        Assert.False(ManagementRules.ValidPeriod(null, date));
    }

    [Theory]
    [InlineData("/management", false, false, "/login")]
    [InlineData("/management/dominio", true, false, "/dashboard")]
    [InlineData("/management/billing", true, false, "/dashboard")]
    [InlineData("/management/calendars", true, false, "/dashboard")]
    [InlineData("/management/reports", true, false, "/dashboard")]
    [InlineData("/onboarding/company", true, false, "/dashboard")]
    [InlineData("/history", true, false, null)]
    [InlineData("/management", true, true, null)]
    [InlineData("/register", false, false, null)]
    public void Routes_enforce_authentication_and_owner_access(string path, bool authenticated, bool owner, string? expected)
        => Assert.Equal(expected, MobileRoutePolicy.Redirect(path, authenticated, owner, Guid.NewGuid()));

    [Fact]
    public void Owner_without_company_resumes_setup()
    {
        Assert.Equal("/onboarding/company", MobileRoutePolicy.Redirect("/dashboard", true, true, Guid.Empty));
        Assert.Null(MobileRoutePolicy.Redirect("/onboarding/company", true, true, Guid.Empty));
    }

    [Fact]
    public void Api_problem_includes_a_short_support_code()
    {
        const string content =
            """{"title":"Localização sem precisão suficiente.","traceId":"abcdef1234567890"}""";

        Assert.Equal("Localização sem precisão suficiente. (código abcdef12)", ManagementRules.ResponseError(content));
    }

    [Fact]
    public async Task Export_error_cannot_be_shared_as_a_file()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        { Content = new StringContent("{\"message\":\"Missing employee mapping\"}") };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => ExportPayload.ReadAsync(response));
        Assert.Equal("Missing employee mapping", error.Message);
    }

    [Fact]
    public async Task Successful_export_preserves_exact_bytes()
    {
        var bytes = Encoding.ASCII.GetBytes("0000000001202609\r\n");
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        Assert.Equal(bytes, await ExportPayload.ReadAsync(response));
    }

    [Fact]
    public async Task Empty_export_is_rejected()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => ExportPayload.ReadAsync(response));
    }
}
