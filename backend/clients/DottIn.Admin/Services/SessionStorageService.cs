using System.Text.Json;
using Microsoft.JSInterop;

namespace DottIn.Admin.Services;

public class SessionStorageService(IJSRuntime js)
{
    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await js.InvokeVoidAsync("sessionStorage.setItem", key, json);
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await js.InvokeAsync<string>("sessionStorage.getItem", key);
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json);
    }

    public Task RemoveItemAsync(string key)
        => js.InvokeVoidAsync("sessionStorage.removeItem", key).AsTask();
}
