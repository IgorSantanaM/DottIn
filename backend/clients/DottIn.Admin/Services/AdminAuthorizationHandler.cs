using System.Net;
using System.Net.Http.Headers;

namespace DottIn.Admin.Services;

public sealed class AdminAuthorizationHandler : DelegatingHandler
{
    private readonly Func<string> getAccessToken;
    private readonly AuthService? authService;

    public AdminAuthorizationHandler(AdminState state, AuthService authService)
        : this(() => state.AccessToken, authService)
    {
    }

    public AdminAuthorizationHandler(Func<string> getAccessToken)
        : this(getAccessToken, null)
    {
    }

    private AdminAuthorizationHandler(Func<string> getAccessToken, AuthService? authService)
    {
        this.getAccessToken = getAccessToken;
        this.authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var isAuthenticationRequest = IsAuthenticationRequest(request);
        if (authService is not null && !isAuthenticationRequest)
            await authService.RefreshIfNeededAsync(cancellationToken: cancellationToken);

        var hasExplicitAuthorization = request.Headers.Authorization is not null;
        AttachCurrentToken(request);

        var canSafelyRetry = request.Method == HttpMethod.Get || request.Method == HttpMethod.Head;
        var retry = authService is null || hasExplicitAuthorization || isAuthenticationRequest || !canSafelyRetry
            ? null
            : await CloneAsync(request, cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || retry is null)
            return response;

        if (!await authService!.RefreshIfNeededAsync(force: true, cancellationToken))
        {
            retry.Dispose();
            return response;
        }

        response.Dispose();
        retry.Headers.Authorization = null;
        AttachCurrentToken(retry);
        return await base.SendAsync(retry, cancellationToken);
    }

    private void AttachCurrentToken(HttpRequestMessage request)
    {
        if (request.Headers.Authorization is not null)
            return;

        var accessToken = getAccessToken();
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static bool IsAuthenticationRequest(HttpRequestMessage request)
        => request.RequestUri?.AbsolutePath.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<HttpRequestMessage> CloneAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}