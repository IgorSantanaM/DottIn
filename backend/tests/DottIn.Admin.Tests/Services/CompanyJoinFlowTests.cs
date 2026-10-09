using System.Net;
using System.Net.Http.Json;
using DottIn.Admin.Models;
using DottIn.Admin.Services;
using Microsoft.JSInterop;

namespace DottIn.Admin.Tests.Services;

public sealed class CompanyJoinFlowTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistingSessionIsValidatedBeforeAuthoritativeMembershipCheck(bool alreadyMember)
    {
        var requests = new List<string>();
        var state = CreateState();
        await state.SetAuthenticatedAsync(Login());
        using var http = Client(request =>
        {
            requests.Add(request.RequestUri!.AbsolutePath);
            return request.RequestUri.AbsolutePath == "/api/auth/refresh"
                ? Json(new RefreshTokenResponse("refreshed", "", DateTime.UtcNow.AddMinutes(15), "Employee"))
                : Json(new CompanyJoinLinkResolutionResponse("Empresa", true, alreadyMember));
        });
        var flow = new CompanyJoinFlow(new AuthService(http, state), state, new AdminApiClient(http, new AdminQueryCache()));

        var result = await flow.ResolveAsync("invite", TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "/api/auth/refresh", "/api/company-join-links/resolve" }, requests);
        Assert.Equal(alreadyMember, result!.AlreadyMember);
        Assert.Equal("refreshed", state.AccessToken);
    }

    [Fact]
    public async Task RevokedSessionIsClearedBeforeAnonymousInviteIsResolved()
    {
        var state = CreateState();
        await state.SetAuthenticatedAsync(Login());
        var resolved = false;
        using var http = Client(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/auth/refresh")
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            resolved = true;
            Assert.False(state.IsAuthenticated);
            return Json(new CompanyJoinLinkResolutionResponse("Empresa", true));
        });

        var result = await new CompanyJoinFlow(new AuthService(http, state), state, new AdminApiClient(http, new AdminQueryCache()))
            .ResolveAsync("invite", TestContext.Current.CancellationToken);

        Assert.True(resolved);
        Assert.False(result!.AlreadyMember);
        Assert.False(state.IsAuthenticated);
    }

    [Fact]
    public async Task TransientSessionFailureDoesNotRenderAnonymousFormsOrConfirmMembership()
    {
        var state = CreateState();
        await state.SetAuthenticatedAsync(Login());
        using var http = Client(request =>
        {
            Assert.Equal("/api/auth/refresh", request.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        var flow = new CompanyJoinFlow(new AuthService(http, state), state, new AdminApiClient(http, new AdminQueryCache()));

        await Assert.ThrowsAsync<HttpRequestException>(() => flow.ResolveAsync("invite", TestContext.Current.CancellationToken));

        Assert.True(state.IsAuthenticated);
        Assert.Equal("access", state.AccessToken);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task InvalidInvitationStaysInvalid(HttpStatusCode status)
    {
        var state = CreateState();
        using var http = Client(_ => new HttpResponseMessage(status));
        var flow = new CompanyJoinFlow(new AuthService(http, state), state, new AdminApiClient(http, new AdminQueryCache()));

        Assert.Null(await flow.ResolveAsync("invalid", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task TransientInviteErrorsAreNotReportedAsExpired(HttpStatusCode status)
    {
        using var http = Client(_ => new HttpResponseMessage(status) { Content = JsonContent.Create(new { message = "Tente novamente" }) });
        var api = new AdminApiClient(http, new AdminQueryCache());

        await Assert.ThrowsAsync<ApiException>(() => api.ResolveCompanyJoinLinkAsync("invite", TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoginUsesPreJoinServerResultAndPreservesEmployeeRole(bool alreadyMember)
    {
        var state = CreateState();
        using var http = Client(request =>
        {
            Assert.Equal("/api/auth/login", request.RequestUri!.AbsolutePath);
            Assert.Contains("X-DottIn-Persist-Session", request.Headers.Select(header => header.Key));
            return Json(Login() with { CompanyJoinAlreadyMember = alreadyMember });
        });

        var result = await new AuthService(http, state).LoginFromCompanyJoinLinkAsync("11122233396", "password", "invite", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(alreadyMember, result.AlreadyMember);
        Assert.Equal("Employee", state.Role);
        Assert.False(state.IsOwner);
    }

    [Fact]
    public async Task OtherCompanyConflictDisplaysServerMessageWithoutLoggingIn()
    {
        var state = CreateState();
        using var http = Client(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(new { message = "Esta conta já pertence a outra empresa." })
        });
        var result = await new AuthService(http, state).LoginFromCompanyJoinLinkAsync("cpf", "password", "invite", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Esta conta já pertence a outra empresa.", result.Error);
        Assert.False(state.IsAuthenticated);
    }

    [Fact]
    public async Task InactiveEmployeeSeesMembershipMessageAndRemainsUnauthenticated()
    {
        var state = CreateState();
        using var http = Client(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = JsonContent.Create(new { code = "employee_inactive", message = "Você não possui mais vínculo ativo com esta empresa." })
        });
        var result = await new AuthService(http, state).LoginAsync("12312312387", "123456");
        Assert.False(result.Success);
        Assert.Equal("Você não possui mais vínculo ativo com esta empresa.", result.Error);
        Assert.False(state.IsAuthenticated);
    }

    [Fact]
    public async Task InvalidCredentialsReturnFriendlyMessageWithoutAnInvalidJsonError()
    {
        var state = CreateState();
        using var http = Client(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var result = await new AuthService(http, state).LoginFromCompanyJoinLinkAsync("cpf", "password", "invite", TestContext.Current.CancellationToken);
        Assert.False(result.Success);
        Assert.Contains("CPF ou senha", result.Error);
        Assert.False(state.IsAuthenticated);
    }

    [Fact]
    public void DashboardNoticeIsConsumedOnlyOnceAndNeverLeaksToAnotherAccount()
    {
        var notice = new CompanyJoinNotice();
        var employeeId = Guid.NewGuid();
        notice.Queue(employeeId);
        Assert.Equal(CompanyJoinNotice.Message, notice.Consume(employeeId));
        Assert.Null(notice.Consume(employeeId));
        notice.Queue(employeeId);
        Assert.Null(notice.Consume(Guid.NewGuid()));
        Assert.Null(notice.Consume(employeeId));
    }

    private static LoginResponse Login() => new("access", "refresh", DateTime.UtcNow.AddMinutes(15),
        new EmployeeInfo(Guid.NewGuid(), "Funcionário", "11122233396", null), Guid.NewGuid(), false, true, "EMPRESA", "Employee");

    private static AdminState CreateState() => new(new SessionStorageService(new MemoryStorage()), new AdminQueryCache());
    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
    private static HttpClient Client(Func<HttpRequestMessage, HttpResponseMessage> response)
        => new(new Handler(response)) { BaseAddress = new Uri("https://test.invalid") };

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(response(request));
        }
    }

    private sealed class MemoryStorage : IJSRuntime
    {
        private readonly Dictionary<string, string> _items = [];
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = (string)args![0]!;
            object? result = null;
            if (identifier.EndsWith("getItem", StringComparison.Ordinal)) _items.TryGetValue(key, out var value);
            else if (identifier.EndsWith("setItem", StringComparison.Ordinal)) _items[key] = (string)args[1]!;
            else if (identifier.EndsWith("removeItem", StringComparison.Ordinal)) _items.Remove(key);
            if (identifier.EndsWith("getItem", StringComparison.Ordinal)) result = _items.GetValueOrDefault(key);
            return ValueTask.FromResult((TValue)result!);
        }
    }
}
