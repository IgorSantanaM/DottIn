using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Http;

namespace DottIn.WebApi.IntegrationTests.Auth;

public sealed class WebSessionCookieTests
{
    [Fact]
    public void HttpsCookieCanBeSentFromCrossSiteAdmin()
    {
        var context = CreateContext("https", "api.example.test");

        WebSessionCookie.Set(context, "refresh-token");

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("DottIn.Refresh=refresh-token", header, StringComparison.Ordinal);
        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=2592000", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocalHttpCookieRemainsUsableAcrossDifferentPorts()
    {
        var context = CreateContext("http", "localhost");

        WebSessionCookie.Set(context, "refresh-token");

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("DottIn.Refresh.Local=refresh-token", header, StringComparison.Ordinal);
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("; secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogoutDeletesSameCookiePath()
    {
        var context = CreateContext("https", "api.example.test");

        WebSessionCookie.Delete(context);

        var header = context.Response.Headers.SetCookie.ToString();
        Assert.Contains("DottIn.Refresh=", header, StringComparison.Ordinal);
        Assert.Contains("path=/", header, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("max-age=2592000", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
    }

    private static DefaultHttpContext CreateContext(string scheme, string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        return context;
    }
}
