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

            return position;
        }
        catch (JSException exception)
        {
            throw new BrowserGeolocationException(
                "Não foi possível obter a localização. Permita o acesso à localização no navegador e tente novamente.",
                exception);
        }
    }
}

public sealed class BrowserGeolocationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
