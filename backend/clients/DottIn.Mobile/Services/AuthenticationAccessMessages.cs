using System.Text.Json;

namespace DottIn.Mobile.Services;

public static class AuthenticationAccessMessages
{
    public const string InactiveEmployee = "Você não possui mais vínculo ativo com esta empresa. Para esclarecer ou reativar seu acesso, entre em contato com o responsável pela empresa.";

    public static string Forbidden(string? content)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                using var json = JsonDocument.Parse(content);
                if (json.RootElement.TryGetProperty("code", out var code) &&
                    code.GetString() == "employee_inactive")
                    return InactiveEmployee;
            }
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
        return "Esta conta não tem acesso à empresa. Entre em contato com o responsável.";
    }
}
