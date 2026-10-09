using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

public sealed class CompanyJoinFlow(AuthService auth, AdminState state, AdminApiClient api)
{
    public async Task<CompanyJoinLinkResolutionResponse?> ResolveAsync(
        string token, CancellationToken cancellationToken = default)
    {
        // A local snapshot alone does not prove that the session is still valid.
        if (state.IsAuthenticated &&
            !await auth.RefreshIfNeededAsync(force: true, cancellationToken: cancellationToken) &&
            state.IsAuthenticated)
            throw new HttpRequestException("Não foi possível validar sua sessão. Tente novamente.");

        return await api.ResolveCompanyJoinLinkAsync(token, cancellationToken);
    }
}
