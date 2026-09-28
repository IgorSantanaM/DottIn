using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DottIn.Mobile.Services.Interfaces;

namespace DottIn.Mobile.Services;

public sealed class MobileTokenRefreshService(
    ISecureStorageService storage,
    IHttpClientFactory httpClientFactory,
    AppState state)
{
    public const string ClientName = "MobileAuthRefresh";
    public const string ExpirationKey = "access_token_expires_at";
    private static readonly TimeSpan RefreshBefore = TimeSpan.FromMinutes(2);
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var accessToken = await storage.GetAsync("access_token");
        if (string.IsNullOrWhiteSpace(accessToken)) return null;
        if (await IsFreshAsync()) return accessToken;
        return await RefreshAsync(accessToken, force: false, cancellationToken);
    }

    public Task<string?> ForceRefreshAsync(string? observedToken, CancellationToken cancellationToken)
        => RefreshAsync(observedToken, force: true, cancellationToken);

    private async Task<string?> RefreshAsync(string? observedToken, bool force, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var currentAccess = await storage.GetAsync("access_token");
            if (string.IsNullOrWhiteSpace(currentAccess)) return null;
            if (currentAccess != observedToken || !force && await IsFreshAsync())
                return currentAccess;

            var refreshToken = await storage.GetAsync("refresh_token");
            if (string.IsNullOrWhiteSpace(refreshToken)) return currentAccess;

            using var client = httpClientFactory.CreateClient(ClientName);
            using var response = await client.PostAsJsonAsync(
                "/api/auth/refresh", new RefreshTokenRequest(refreshToken), cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                await ClearRejectedSessionAsync();
                return null;
            }

            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<TokenResponse>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
            if (updated is null || string.IsNullOrWhiteSpace(updated.AccessToken) ||
                string.IsNullOrWhiteSpace(updated.RefreshToken))
                throw new InvalidDataException("Resposta de renovação de sessão inválida.");

            await storage.SetAsync("access_token", updated.AccessToken);
            await storage.SetAsync("refresh_token", updated.RefreshToken);
            await storage.SetAsync(ExpirationKey,
                updated.ExpiresAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            return updated.AccessToken;
        }
        finally { gate.Release(); }
    }

    private async Task<bool> IsFreshAsync()
    {
        var storedExpiry = await storage.GetAsync(ExpirationKey);
        return DateTime.TryParse(storedExpiry, CultureInfo.InvariantCulture,
                   DateTimeStyles.RoundtripKind, out var expiresAt) &&
               expiresAt.ToUniversalTime() > DateTime.UtcNow.Add(RefreshBefore);
    }

    private async Task ClearRejectedSessionAsync()
    {
        await storage.RemoveAsync("access_token");
        await storage.RemoveAsync("refresh_token");
        await storage.RemoveAsync(ExpirationKey);
        await storage.RemoveAsync("mobile_session");
        state.Logout();
    }
}
