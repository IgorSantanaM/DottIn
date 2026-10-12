using DottIn.Mobile.Services.Interfaces;

namespace DottIn.Mobile.Services;

public sealed class MobileAppearanceService(ISecureStorageService storage, AppState state)
{
    public async Task RestoreAsync(bool systemDark)
        => state.SetTheme(await storage.GetAsync($"theme:{state.EmployeeId}") ?? "system", systemDark);

    public async Task SetAsync(string preference, bool systemDark)
    {
        if (preference is not ("light" or "dark" or "system")) throw new ArgumentException("Tema inválido.");
        await storage.SetAsync($"theme:{state.EmployeeId}", preference);
        state.SetTheme(preference, systemDark);
    }
}
