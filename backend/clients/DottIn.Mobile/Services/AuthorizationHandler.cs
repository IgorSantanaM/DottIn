using System.Net.Http.Headers;
using DottIn.Mobile.Services.Interfaces;

namespace DottIn.Mobile.Services;

public class AuthorizationHandler : DelegatingHandler
{
    private readonly ISecureStorageService _secureStorage;
    private readonly IAuthApi _authApi;

    public AuthorizationHandler(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
        _authApi = null!; // Will be resolved per-request
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _secureStorage.GetAsync("access_token");

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 - try to refresh token
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshToken = await _secureStorage.GetAsync("refresh_token");

            if (!string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    // Note: In real implementation, use a separate HTTP client for refresh
                    // to avoid circular dependency
                    var newToken = await RefreshTokenAsync(refreshToken);

                    if (newToken != null)
                    {
                        await _secureStorage.SetAsync("access_token", newToken.AccessToken);
                        await _secureStorage.SetAsync("refresh_token", newToken.RefreshToken);

                        // Retry the original request
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken.AccessToken);
                        response = await base.SendAsync(request, cancellationToken);
                    }
                }
                catch
                {
                    // Refresh failed - user needs to re-login
                    await _secureStorage.ClearAllAsync();
                }
            }
        }

        return response;
    }

    private async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        // This is a simplified version - in production, use a separate HTTP client
        // to avoid circular dependencies with the authorization handler
        return null;
    }
}
