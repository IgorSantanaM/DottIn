using System.Net.Http.Json;
using DottIn.Admin.Models;

namespace DottIn.Admin.Services;

public class AuthService(HttpClient http, AdminState state)
{
    public async Task InitializeAsync()
    {
        var session = await state.GetSessionAsync();
        if (session is null) return;

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            var refreshed = await RefreshAsync(session.RefreshToken);
            if (refreshed is null)
            {
                await state.LogoutAsync();
                return;
            }

            session = session with
            {
                AccessToken = refreshed.AccessToken,
                RefreshToken = refreshed.RefreshToken,
                ExpiresAt = refreshed.ExpiresAt
            };
        }

        await state.SetAuthenticatedAsync(session);
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.AccessToken);
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string cpf, string password, string companyCode)
    {
        try
        {
            var request = new LoginRequest(cpf, password, companyCode);
            var response = await http.PostAsJsonAsync("/api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "CPF ou senha incorretos"
                    : "Empresa não encontrada";
                return (false, error);
            }

            var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (login is null) return (false, "Resposta inválida do servidor");

            await state.SetAuthenticatedAsync(login, companyCode);
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);

            return (true, null);
        }
        catch (HttpRequestException)
        {
            return (false, "Não foi possível conectar ao servidor");
        }
    }

    public async Task<(bool Success, EmployeeInfo? Employee, string? Error)> IdentifyWithPasswordAsync(
        string cpf, string password, string companyCode)
    {
        try
        {
            var request = new LoginRequest(cpf, password, companyCode);
            var response = await http.PostAsJsonAsync("/api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "CPF ou senha incorretos"
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

    public async Task<(bool Success, EmployeeInfo? Employee, string? Error)> IdentifyWithPinAsync(
        string cpf, string pin, string companyCode)
    {
        try
        {
            var request = new PinLoginRequest(cpf, pin, companyCode);
            var response = await http.PostAsJsonAsync("/api/auth/login/pin", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
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

    private async Task<RefreshTokenResponse?> RefreshAsync(string refreshToken)
    {
        try
        {
            var response = await http.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(refreshToken));
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
