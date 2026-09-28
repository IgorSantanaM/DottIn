using System.Net;
using System.Net.Http.Headers;

namespace DottIn.Mobile.Services;

public class AuthorizationHandler(MobileTokenRefreshService tokens) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (IsPublicAuthRequest(request))
            return await base.SendAsync(request, cancellationToken);

        var hasExplicitAuthorization = request.Headers.Authorization is not null;
        var accessToken = hasExplicitAuthorization
            ? null
            : await tokens.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || hasExplicitAuthorization ||
            string.IsNullOrWhiteSpace(accessToken) ||
            request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
            return response;

        // Safe reads may retry after a 401. Mutations are refreshed before sending,
        // never replayed automatically because their outcome may be ambiguous.
        var refreshed = await tokens.ForceRefreshAsync(accessToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(refreshed) || refreshed == accessToken)
            return response;

        response.Dispose();
        using var retry = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers.Where(header => header.Key != "Authorization"))
            retry.Headers.TryAddWithoutValidation(header.Key, header.Value);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed);
        return await base.SendAsync(retry, cancellationToken);
    }

    private static bool IsPublicAuthRequest(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        return path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/api/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
               path.Equals("/api/auth/register/owner", StringComparison.OrdinalIgnoreCase);
    }
}