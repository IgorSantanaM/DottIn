using System.Net;
using System.Net.Http.Headers;
using DottIn.Admin.Services;

namespace DottIn.Admin.Tests.Services;

public sealed class AdminAuthorizationHandlerTests
{
    [Fact]
    public async Task SendAsync_AttachesCurrentBearerToken_WhenRequestHasNoAuthorizationHeader()
    {
        var terminalHandler = new RecordingHandler();
        using var authorizationHandler = new AdminAuthorizationHandler(() => "synthetic-access-token")
        {
            InnerHandler = terminalHandler
        };
        using var client = new HttpClient(authorizationHandler)
        {
            BaseAddress = new Uri("https://api.example.test")
        };

        await client.GetAsync("/api/timekeeping/employee/test/history", TestContext.Current.CancellationToken);

        Assert.NotNull(terminalHandler.Authorization);
        Assert.Equal("Bearer", terminalHandler.Authorization!.Scheme);
        Assert.Equal("synthetic-access-token", terminalHandler.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_PreservesExplicitAuthorizationHeader()
    {
        var terminalHandler = new RecordingHandler();
        using var authorizationHandler = new AdminAuthorizationHandler(() => "state-token")
        {
            InnerHandler = terminalHandler
        };
        using var client = new HttpClient(authorizationHandler)
        {
            BaseAddress = new Uri("https://api.example.test")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/timekeeping/employee/test/history");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "explicit-token");

        await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("explicit-token", terminalHandler.Authorization?.Parameter);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
