namespace DottIn.Domain.Storage
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);
        Task<Stream> DownloadAsync(string fileIdentifier, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(string fileIdentifier, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string fileIdentifier, CancellationToken cancellationToken = default);
        string GetReadOnlyUrl(string fileIdentifier, int expiryMinutes = 6, CancellationToken cancellationToken = default);
    }
}
