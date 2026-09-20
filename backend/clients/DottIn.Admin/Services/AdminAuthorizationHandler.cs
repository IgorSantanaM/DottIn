using System.Net.Http.Headers;

namespace DottIn.Admin.Services;

public sealed class AdminAuthorizationHandler : DelegatingHandler
{
    private readonly Func<string> getAccessToken;

    public AdminAuthorizationHandler(AdminState state)
        : this(() => state.AccessToken)
    {
    }

    public AdminAuthorizationHandler(Func<string> getAccessToken)
    {
        this.getAccessToken = getAccessToken;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var accessToken = getAccessToken();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
