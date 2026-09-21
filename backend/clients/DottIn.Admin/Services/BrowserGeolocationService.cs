using DottIn.Admin.Models;
using Microsoft.JSInterop;

namespace DottIn.Admin.Services;

public sealed class BrowserGeolocationService(IJSRuntime js)
{
    public async Task<GeolocationInfo> GetCurrentPositionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var position = await js.InvokeAsync<GeolocationInfo>("dottin.getCurrentPosition", cancellationToken);

            if (position.Latitude == 0 && position.Longitude == 0)
                throw new BrowserGeolocationException("Não foi possível obter uma localização válida para a filial.");
            if (position.AccuracyMeters is null or <= 0 or > 100)
                throw new BrowserGeolocationException(
                    "A localização está imprecisa. Aguarde alguns segundos em uma área aberta e tente novamente.");

            return position;
        }
        catch (JSException exception)
        {
            var message = exception.Message switch
            {
                var value when value.Contains("Permita o acesso", StringComparison.OrdinalIgnoreCase) =>
                    "Permita o acesso à localização no navegador para registrar o ponto.",
                var value when value.Contains("não está disponível", StringComparison.OrdinalIgnoreCase) =>
                    "A localização não está disponível. Verifique se o GPS está ativo e tente novamente.",
                var value when value.Contains("expirou", StringComparison.OrdinalIgnoreCase) =>
                    "A obtenção da localização expirou. Vá para uma área com melhor sinal e tente novamente.",
                var value when value.Contains("não é compatível", StringComparison.OrdinalIgnoreCase) =>
                    "Este navegador não oferece localização compatível com o registro de ponto.",
                _ => "Não foi possível obter a localização. Verifique a permissão do navegador e tente novamente."
            };
            throw new BrowserGeolocationException(message, exception);
        }
    }
}

public sealed class BrowserGeolocationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
