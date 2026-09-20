using System.Globalization;
using System.Text.Json;

namespace DottIn.Mobile.Services;

public static class ManagementRules
{
    public static string Digits(string value) => new(value.Where(c => c is >= '0' and <= '9').ToArray());
    public static bool NumericCode(string value, int length) => !string.IsNullOrWhiteSpace(value)
        && value.Length <= length && value.All(c => c is >= '0' and <= '9') && value.Any(c => c != '0');
    public static bool ValidPeriod(DateTime? start, DateTime? end) => start.HasValue && end.HasValue && start.Value.Date <= end.Value.Date;
    public static string Hours(TimeSpan value) => $"{(long)value.TotalHours:D2}:{value.Minutes:D2}";
    public static string Limit(int count) => count <= 0 ? "Ilimitado" : count.ToString(CultureInfo.InvariantCulture);
    public static string Error(Exception exception)
    {
        if (exception is Refit.ApiException api)
        {
            if (api.StatusCode == System.Net.HttpStatusCode.Unauthorized) return "Sessão expirada. Entre novamente.";
            if (api.StatusCode == System.Net.HttpStatusCode.Forbidden) return "Você não tem permissão para esta ação.";
            return ResponseError(api.Content);
        }
        return exception is InvalidOperationException ? exception.Message : "Não foi possível concluir. Verifique a conexão e tente novamente.";
    }
    public static string ResponseError(string? content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.String) return doc.RootElement.GetString()!;
                foreach (var property in doc.RootElement.EnumerateObject())
                    if (property.Name.Equals("message", StringComparison.OrdinalIgnoreCase)) return property.Value.ToString();
                foreach (var property in doc.RootElement.EnumerateObject())
                    if (property.Name.Equals("errors", StringComparison.OrdinalIgnoreCase)) return property.Value.ToString();
            }
            catch (JsonException) { }
            catch (InvalidOperationException) { }
        }
        return "Não foi possível concluir a operação. Confira os dados e tente novamente.";
    }
}
