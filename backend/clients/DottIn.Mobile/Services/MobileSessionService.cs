using System.Text.Json;
using DottIn.Mobile.Services.Interfaces;

namespace DottIn.Mobile.Services;

public sealed class MobileSessionService(IAuthApi auth, IBranchApi branches, ISecureStorageService storage, AppState state)
{
    private LoginResponse? _session;
    private bool _initialized;
    private bool _remember = true;
    public string LandingPage => state.IsOwner && state.BranchId == Guid.Empty ? "/onboarding/company" : "/dashboard";
    public async Task AcceptAsync(LoginResponse response, bool remember = true)
    {
        _remember = remember;
        _session = response;
        await PersistAsync();
        if (response.IsOwner)
        {
            var available = (await branches.GetByOwnerAsync(response.Employee.Id)).Where(b => b.IsActive).ToList();
            var selected = available.FirstOrDefault(b => b.Id == response.BranchId)
                ?? available.FirstOrDefault(b => b.IsHeadquarters) ?? available.FirstOrDefault();
            if (selected is not null)
            {
                var details = await branches.GetByIdAsync(selected.Id);
                _session = response with { BranchId = selected.Id, CompanyCode = details.CompanyCode,
                    IsHeadquarters = available.Any(b => b.IsHeadquarters) };
            }
            else _session = response with { BranchId = Guid.Empty, CompanyCode = "", IsHeadquarters = false };
            state.SetAvailableBranches(available.Select(b => new BranchOption(b.Id, b.Name, b.IsHeadquarters)));
        }
        else state.SetAvailableBranches([]);
        await PersistAsync();
        await storage.SetAsync("company_code", _session.CompanyCode);
        state.SetAuthenticated(_session.Employee, _session.BranchId, _session.IsOwner, _session.IsHeadquarters);
        _initialized = true;
    }
    public async Task RestoreAsync()
    {
        if (_initialized) return;
        var saved = await storage.GetAsync("mobile_session");
        if (string.IsNullOrEmpty(saved))
        {
            await storage.RemoveAsync("access_token");
            await storage.RemoveAsync("refresh_token");
            state.Logout();
            _initialized = true;
            return;
        }
        try
        {
            var session = JsonSerializer.Deserialize<LoginResponse>(saved);
            if (session is null) { await ClearAsync(); return; }
            var token = await storage.GetAsync("refresh_token");
            if (string.IsNullOrEmpty(token)) { await ClearAsync(); return; }
            var refreshed = await auth.RefreshTokenAsync(new RefreshTokenRequest(token));
            await AcceptAsync(session with { AccessToken = refreshed.AccessToken, RefreshToken = refreshed.RefreshToken, ExpiresAt = refreshed.ExpiresAt });
        }
        catch (Refit.ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        { await ClearAsync(); }
        catch (JsonException) { await ClearAsync(); }
    }
    public async Task SwitchBranchAsync(Guid id)
    {
        if (_session is null || !state.CanSwitchBranches || !state.AvailableBranches.Any(b => b.Id == id))
            throw new InvalidOperationException("Filial indisponível.");
        var details = await branches.GetByIdAsync(id);
        _session = _session with { BranchId = id, CompanyCode = details.CompanyCode };
        await PersistAsync();
        await storage.SetAsync("company_code", details.CompanyCode);
        state.SwitchBranch(id);
    }
    public async Task CompanyCreatedAsync(CreateBranchResponse branch)
    {
        if (_session is null) throw new InvalidOperationException("Entre novamente para continuar.");
        await AcceptAsync(_session with { BranchId = branch.BranchId, CompanyCode = branch.CompanyCode, IsHeadquarters = true }, _remember);
    }
    private async Task PersistAsync()
    {
        if (_session is null) return;
        await storage.SetAsync("access_token", _session.AccessToken);
        await storage.SetAsync("refresh_token", _session.RefreshToken);
        if (_remember) await storage.SetAsync("mobile_session", JsonSerializer.Serialize(_session));
        else await storage.RemoveAsync("mobile_session");
    }
    public async Task ClearAsync()
    {
        _session = null;
        _initialized = true;
        await storage.RemoveAsync("mobile_session");
        await storage.RemoveAsync("access_token");
        await storage.RemoveAsync("refresh_token");
        state.Logout();
    }
}
