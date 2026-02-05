using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DottIn.Domain.Storage;

namespace DottIn.Infra.Services.Storage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;

        public FileStorageService(string connectionString, string containerName)
        {
            var serviceClient = new BlobServiceClient(connectionString);
            _blobContainerClient = serviceClient.GetBlobContainerClient(containerName);
            _blobContainerClient.CreateIfNotExists(PublicAccessType.None);
        }
        public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(fileName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };

            await blobClient.UploadAsync(stream, options, cancellationToken);

            return blobClient.Uri.ToString();
        }

        public async Task<Stream> DownloadAsync(string fileIdentifier, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(fileIdentifier);
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return response.Value.Content;
        }

        public async Task<bool> DeleteAsync(string fileIdentifier, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(fileIdentifier);
            return await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        }

        public async Task<bool> ExistsAsync(string fileIdentifier, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(fileIdentifier);
            return await blobClient.ExistsAsync(cancellationToken);
        }

        public string GetReadOnlyUrl(string fileIdentifier, int expiryMinutes = 6, CancellationToken cancellationToken = default)
        {
            var blobClient = _blobContainerClient.GetBlobClient(fileIdentifier);

            if (!blobClient.CanGenerateSasUri)
                throw new InvalidOperationException("BlobClient cannot generate SAS URI. Check permissions/connection string.");

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _blobContainerClient.Name,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

    }
}
