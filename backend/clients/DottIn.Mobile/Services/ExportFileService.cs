namespace DottIn.Mobile.Services;

public class ExportFileService
{
    public async Task ShareAsync(HttpResponseMessage response, string fileName)
    {
        using (response)
        {
            var bytes = await ExportPayload.ReadAsync(response);
            var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}");
            await File.WriteAllBytesAsync(path, bytes);
            await Share.RequestAsync(new ShareFileRequest { Title = "Exportar registros", File = new ShareFile(path) });
        }
    }
}
