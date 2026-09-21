using DottIn.Admin.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DottIn.Admin.Services;

public class AuthService(HttpClient http, AdminState state)
{
    private const string PersistSessionHeader = "X-DottIn-Persist-Session";
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(2);
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    public Task<bool> RestoreLocalSessionAsync()
        => state.RestoreSnapshotAsync();

    public async Task ValidateRestoredSessionAsync(CancellationToken cancellationToken = default)
    {
        if (!state.IsAuthenticated)
            return;

        var attempt = await RefreshAsync(cancellationToken);
        if (attempt.Response is not null)
        {
            await state.CompleteRefreshAsync(attempt.Response);
            return;
        }

        if (attempt.IsInvalid)
            await state.LogoutAsync();
        else
            state.MarkSessionRestoreFailed("Não foi possível confirmar sua sessão. Verifique a conexão e tente novamente.");
    }

    public async Task<bool> RefreshIfNeededAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (!state.IsAuthenticated)
            return false;

        if (!force && state.IsSessionReady && state.ExpiresAt > DateTime.UtcNow.Add(RefreshWindow))
            return true;

        var observedExpiration = state.ExpiresAt;
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (state.IsSessionReady && state.ExpiresAt != observedExpiration)
                return true;

            if (!force && state.IsSessionReady && state.ExpiresAt > DateTime.UtcNow.Add(RefreshWindow))
                return true;

            var attempt = await RefreshAsync(cancellationToken);
            if (attempt.Response is not null)
            {
                await state.CompleteRefreshAsync(attempt.Response);
                return true;
            }

            if (attempt.IsInvalid)
                await state.LogoutAsync();
            else
                state.MarkSessionRestoreFailed("A conexão foi interrompida. Seus dados locais foram preservados.");

            return false;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public async Task<(bool Success, string? Error)> LoginAsync(
        string cpf,
        string password,
        string? companyJoinToken = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new LoginRequest(cpf, password, companyJoinToken))
            };
            request.Headers.TryAddWithoutValidation(PersistSessionHeader, "true");
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            using var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var validation = JsonSerializer.Deserialize<ValidationErrorResponse>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return (false, validation?.Errors.Select(x => x.Error).FirstOrDefault()
                    ?? "Não foi possível validar o acesso.");
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (login is null)
                return (false, "Resposta inválida do servidor");

            await state.SetAuthenticatedAsync(login);
            return (true, null);
        }
        catch (HttpRequestException)
        {
            return (false, "Não foi possível conectar ao servidor");
        }
    }

    public async Task<(bool Success, EmployeeInfo? Employee, string? Error)> IdentifyWithPasswordAsync(
        string cpf,
        string password,
        string companyCode)
    {
        try
        {
            var response = await http.PostAsJsonAsync("/api/auth/login", new LoginRequest(cpf, password));
            if (!response.IsSuccessStatusCode)
            {
                var error = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "CPF ou senha incorretos"
                    : "Funcionário não encontrado";
                return (false, null, error);
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (login is null)
                return (false, null, "Resposta inválida");

            return string.Equals(login.CompanyCode, companyCode, StringComparison.OrdinalIgnoreCase)
                ? (true, login.Employee, null)
                : (false, null, "Funcionário não pertence a esta empresa");
        }
        catch (HttpRequestException)
        {
            return (false, null, "Erro de conexão");
        }
    }

    public async Task<(bool Success, EmployeeInfo? Employee, string? Error)> IdentifyWithPinAsync(
        string cpf,
        string pin,
        string companyCode)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                "/api/auth/login/pin",
                new PinLoginRequest(cpf, pin, companyCode));

            if (!response.IsSuccessStatusCode)
            {
                var error = response.StatusCode == HttpStatusCode.Unauthorized
                    ? "PIN incorreto"
                    : "Funcionário não encontrado";
                return (false, null, error);
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return login is not null
                ? (true, login.Employee, null)
                : (false, null, "Resposta inválida");
        }
        catch (HttpRequestException)
        {
            return (false, null, "Erro de conexão");
        }
    }

    private async Task<RefreshAttempt> RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh")
            {
                Content = JsonContent.Create(new RefreshTokenRequest(null))
            };
            request.Headers.TryAddWithoutValidation(PersistSessionHeader, "true");
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new RefreshAttempt(null, IsInvalid: true);
            if (!response.IsSuccessStatusCode)
                return new RefreshAttempt(null, IsInvalid: false);

            return new RefreshAttempt(
                await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken),
                IsInvalid: false);
        }
        catch (HttpRequestException)
        {
            return new RefreshAttempt(null, IsInvalid: false);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new RefreshAttempt(null, IsInvalid: false);
        }
    }

    public sealed class ValidationErrorResponse
    {
        public int Status { get; set; }
        public string? Title { get; set; }
        public List<FieldError> Errors { get; set; } = [];
    }

    public sealed class FieldError
    {
        public string? Field { get; set; }
        public string? Error { get; set; }
    }

    private sealed record RefreshAttempt(RefreshTokenResponse? Response, bool IsInvalid);
}