using System.Text.Json;
using Microsoft.JSInterop;

namespace DottIn.Admin.Services;

public class SessionStorageService(IJSRuntime js)
{
    private readonly Dictionary<string, string> memory = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> sessionMemory = new(StringComparer.Ordinal);
    private bool localStorageAvailable = true;
    private bool sessionStorageAvailable = true;

    public bool IsPersistent => localStorageAvailable;

    public async Task SetItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        memory[key] = json;
        if (!localStorageAvailable)
            return;

        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch (JSException)
        {
            localStorageAvailable = false;
        }
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        string? json = null;
        if (localStorageAvailable)
        {
            try
            {
                json = await js.InvokeAsync<string?>("localStorage.getItem", key);
                if (json is not null)
                    memory[key] = json;
            }
            catch (JSException)
            {
                localStorageAvailable = false;
            }
        }

        if (!localStorageAvailable)
            memory.TryGetValue(key, out json);

        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetSessionItemAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        sessionMemory[key] = json;
        if (!sessionStorageAvailable)
            return;

        try
        {
            await js.InvokeVoidAsync("sessionStorage.setItem", key, json);
        }
        catch (JSException)
        {
            sessionStorageAvailable = false;
        }
    }

    public async Task<T?> GetSessionItemAsync<T>(string key)
    {
        string? json = null;
        if (sessionStorageAvailable)
        {
            try
            {
                json = await js.InvokeAsync<string?>("sessionStorage.getItem", key);
                if (json is not null)
                    sessionMemory[key] = json;
            }
            catch (JSException)
            {
                sessionStorageAvailable = false;
            }
        }

        if (!sessionStorageAvailable)
            sessionMemory.TryGetValue(key, out json);

        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json);
    }

    public async Task RemoveItemAsync(string key)
    {
        memory.Remove(key);
        sessionMemory.Remove(key);
        if (localStorageAvailable)
        {
            try
            {
                await js.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (JSException)
            {
                localStorageAvailable = false;
            }
        }

        if (sessionStorageAvailable)
        {
            try
            {
                await js.InvokeVoidAsync("sessionStorage.removeItem", key);
            }
            catch (JSException)
            {
                sessionStorageAvailable = false;
            }
        }
    }
}
