using Microsoft.JSInterop;

namespace DottIn.Admin.Services;

public sealed class BrowserDownloadService(IJSRuntime js)
{
    public async Task DownloadAsync(
        byte[] bytes,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        using var stream = new MemoryStream(bytes, writable: false);
        using var reference = new DotNetStreamReference(stream);
        await js.InvokeVoidAsync(
            "dottin.downloadFile",
            cancellationToken,
            fileName,
            contentType,
            reference);
    }
}
