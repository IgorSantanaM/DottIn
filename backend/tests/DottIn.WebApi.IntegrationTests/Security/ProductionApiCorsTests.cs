using System.Net;
using DottIn.Infra.Data.Contexts;
using DottIn.Presentation.WebApi.Health;
using DottIn.Presentation.WebApi.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DottIn.WebApi.IntegrationTests.Security;

public sealed class ProductionApiCorsTests
{
    private const string Origin = "https://dottin.cloudlane.com";

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/")]
    [InlineData("/api/health/live")]
    [InlineData("/health/live")]
    public async Task ApiRootsAndLiveness_AreRoutableAndExposeAllowedOrigin(string path)
    {
        await using var app = await CreateApplicationAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Origin", Origin);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
    }

    [Fact]
    public async Task Preflight_AllowsCredentialedApiRequestsWithAuthorizationAndJson()
    {
        await using var app = await CreateApplicationAsync();
        using var client = app.GetTestClient();
        using var request = CreatePreflight(Origin);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(Origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("POST", response.Headers.GetValues("Access-Control-Allow-Methods"));
        var headers = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers"));
        Assert.Contains("authorization", headers, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("content-type", headers, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://untrusted.example")]
    [InlineData("http://dottin.cloudlane.com")]
    [InlineData("https://dottin.cloudlane.com.untrusted.example")]
    public async Task Preflight_DoesNotAuthorizeUnlistedOrigins(string origin)
    {
        await using var app = await CreateApplicationAsync();
        using var client = app.GetTestClient();
        using var request = CreatePreflight(origin);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task UnknownApiRoute_Remains404InsteadOfServingTheSpa()
    {
        await using var app = await CreateApplicationAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/unknown.json");
        request.Headers.Add("Origin", Origin);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(Origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.cloudlane.com")]
    [InlineData("https://dottin.cloudlane.com/api")]
    [InlineData("https://dottin.cloudlane.com?secret=value")]
    [InlineData("https://user:password@dottin.cloudlane.com")]
    public void Policy_RejectsWildcardsAndNonOriginUrls(string value)
        => Assert.Throws<InvalidOperationException>(() => CorsPolicyFactory.Create([value]));

    [Fact]
    public void Policy_NormalizesTrailingSlashWithoutEnablingEveryOrigin()
    {
        var policy = CorsPolicyFactory.Create([Origin + "/", Origin]);

        Assert.Equal(Origin, Assert.Single(policy.Origins));
        Assert.False(policy.AllowAnyOrigin);
        Assert.True(policy.SupportsCredentials);
    }

    private static HttpRequestMessage CreatePreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return request;
    }

    private static async Task<WebApplication> CreateApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseTestServer();
        builder.Services.AddDbContext<DottInContext>(options =>
            options.UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused"));
        builder.Services.AddCors(options => options.AddDefaultPolicy(CorsPolicyFactory.Create([Origin])));
        var app = builder.Build();
        app.UseRouting();
        app.UseCors();
        HealthEndpoints.Map(app);
        // Tests preflight middleware only; the real login handler is smoke-tested through Docker.
        app.MapPost("/api/auth/login", () => Results.BadRequest());
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
