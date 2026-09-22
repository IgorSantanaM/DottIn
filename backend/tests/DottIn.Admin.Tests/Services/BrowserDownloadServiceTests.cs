using DottIn.Admin.Services;
using Microsoft.JSInterop;

namespace DottIn.Admin.Tests.Services;

public sealed class BrowserDownloadServiceTests
{
    [Fact]
    public async Task SendsBytesAsStreamWithFileMetadata()
    {
        var js = new CaptureJsRuntime();
        var service = new BrowserDownloadService(js);
        var data = new byte[] { 0, 1, 127, 255 };

        await service.DownloadAsync(data, "registros.csv", "text/csv;charset=utf-8", TestContext.Current.CancellationToken);

        Assert.Equal("dottin.downloadFile", js.Identifier);
        Assert.Equal("registros.csv", js.FileName);
        Assert.Equal("text/csv;charset=utf-8", js.ContentType);
        Assert.Equal(data, js.Content);
    }

    [Fact]
    public async Task RejectsMissingFileMetadata()
    {
        var service = new BrowserDownloadService(new CaptureJsRuntime());

        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadAsync([1], "", "text/csv", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadAsync([1], "report.csv", "", TestContext.Current.CancellationToken));
    }

    private sealed class CaptureJsRuntime : IJSRuntime
    {
        public string? Identifier { get; private set; }
        public string? FileName { get; private set; }
        public string? ContentType { get; private set; }
        public byte[]? Content { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Identifier = identifier;
            FileName = Assert.IsType<string>(args![0]);
            ContentType = Assert.IsType<string>(args[1]);
            var reference = Assert.IsType<DotNetStreamReference>(args[2]);
            using var output = new MemoryStream();
            await reference.Stream.CopyToAsync(output, cancellationToken);
            Content = output.ToArray();
            return default!;
        }
    }
}
