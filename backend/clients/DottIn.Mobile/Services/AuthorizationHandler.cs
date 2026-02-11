using System.Net.Http.Headers;
using DottIn.Mobile.Services.Interfaces;

namespace DottIn.Mobile.Services;

public class AuthorizationHandler : DelegatingHandler
{
    private readonly ISecureStorageService _secureStorage;

    public AuthorizationHandler(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _secureStorage.GetAsync("access_token");

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Note: Token refresh logic removed to avoid circular dependency
            // Implement refresh in the component/service that needs it
            
            return response;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! AuthorizationHandler error: {ex.Message}");
            throw;
        }
    }
}
