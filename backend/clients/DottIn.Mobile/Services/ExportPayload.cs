namespace DottIn.Mobile.Services;

public static class ExportPayload
{
    public static async Task<byte[]> ReadAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(ManagementRules.ResponseError(await response.Content.ReadAsStringAsync()));
        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (bytes.Length == 0) throw new InvalidOperationException("Nenhum dado para exportar.");
        return bytes;
    }
}
