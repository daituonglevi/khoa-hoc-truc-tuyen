using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ELearningWebsite.Models;
using Microsoft.Extensions.Options;

namespace ELearningWebsite.Services
{
    public class AzureBlobPrivateStorageService : IPrivateBlobStorageService
    {
        private readonly BlobStorageSettings _settings;
        private readonly BlobContainerClient? _containerClient;
        private readonly bool _isConfigured;

        public AzureBlobPrivateStorageService(IOptions<BlobStorageSettings> settings)
        {
            _settings = settings.Value;

            _isConfigured = !string.IsNullOrWhiteSpace(_settings.ConnectionString);

            if (string.IsNullOrWhiteSpace(_settings.ContainerName))
            {
                _settings.ContainerName = "private-media";
            }

            if (_isConfigured)
            {
                _containerClient = new BlobContainerClient(_settings.ConnectionString, _settings.ContainerName);
            }
        }

        public async Task<PrivateBlobUploadResult> UploadAsync(IFormFile file, int ownerUserId, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();
            await EnsureContainerExistsAsync(cancellationToken);

            if (file == null || file.Length <= 0)
            {
                throw new InvalidOperationException("File upload is empty.");
            }

            ValidateFile(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blobName = $"{ownerUserId}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
            var blobClient = _containerClient.GetBlobClient(blobName);
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(
                    stream,
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = contentType
                        }
                    },
                    cancellationToken);
            }

            return new PrivateBlobUploadResult
            {
                BlobName = blobName,
                BlobPath = $"{_settings.ContainerName}/{blobName}",
                SizeBytes = file.Length,
                ContentType = contentType
            };
        }

        public async Task<string> GenerateReadUrlAsync(string blobName, int? expiryMinutes = null, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();
            await EnsureContainerExistsAsync(cancellationToken);

            var blobClient = _containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                throw new FileNotFoundException("Blob not found.", blobName);
            }

            if (!blobClient.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Blob connection does not support SAS generation. Use an account key connection string.");
            }

            var ttlMinutes = expiryMinutes.GetValueOrDefault(_settings.ReadSasMinutes);
            if (ttlMinutes <= 0)
            {
                ttlMinutes = 30;
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _settings.ContainerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(ttlMinutes)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        public async Task<bool> DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();
            await EnsureContainerExistsAsync(cancellationToken);
            var blobClient = _containerClient.GetBlobClient(blobName);
            var deleted = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
            return deleted.Value;
        }

        private async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
        {
            EnsureConfigured();
            await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        }

        private void EnsureConfigured()
        {
            if (!_isConfigured || _containerClient == null)
            {
                throw new InvalidOperationException("BlobStorage:ConnectionString chưa được cấu hình.");
            }
        }

        private void ValidateFile(IFormFile file)
        {
            var maxBytes = (long)_settings.MaxFileSizeMb * 1024 * 1024;
            if (maxBytes > 0 && file.Length > maxBytes)
            {
                throw new InvalidOperationException($"File exceeds max size {_settings.MaxFileSizeMb} MB.");
            }

            if (_settings.AllowedExtensions != null && _settings.AllowedExtensions.Count > 0)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowed = _settings.AllowedExtensions
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim().ToLowerInvariant())
                    .ToHashSet();

                if (!allowed.Contains(extension))
                {
                    throw new InvalidOperationException($"File extension '{extension}' is not allowed.");
                }
            }
        }
    }
}