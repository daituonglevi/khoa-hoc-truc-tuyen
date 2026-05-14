namespace ELearningWebsite.Services
{
    public interface IPrivateBlobStorageService
    {
        Task<PrivateBlobUploadResult> UploadAsync(IFormFile file, int ownerUserId, CancellationToken cancellationToken = default);
        Task<string> GenerateReadUrlAsync(string blobName, int? expiryMinutes = null, CancellationToken cancellationToken = default);
        Task<bool> DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default);
    }

    public class PrivateBlobUploadResult
    {
        public string BlobName { get; set; } = string.Empty;
        public string BlobPath { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
    }
}