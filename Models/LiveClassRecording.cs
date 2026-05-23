using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    /// <summary>
    /// Recording metadata for live classes
    /// </summary>
    public class LiveClassRecording
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("LiveClass")]
        public int LiveClassId { get; set; }

        /// <summary>
        /// Recording ID from Zoom (or external storage provider)
        /// </summary>
        [StringLength(100)]
        public string? ExternalRecordingId { get; set; }

        /// <summary>
        /// Recording file URL (Zoom cloud or our blob storage)
        /// </summary>
        [Required]
        public string RecordingUrl { get; set; } = string.Empty;

        /// <summary>
        /// Duration in seconds
        /// </summary>
        public long? DurationSeconds { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long? FileSizeBytes { get; set; }

        /// <summary>
        /// Video format (mp4, m3u8, etc)
        /// </summary>
        [StringLength(20)]
        public string? Format { get; set; }

        /// <summary>
        /// Is this recording public or restricted?
        /// </summary>
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// Can students download the recording?
        /// </summary>
        public bool IsDownloadable { get; set; } = false;

        /// <summary>
        /// Expiration date (null = no expiration)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Provider: Zoom, AzureBlob, etc
        /// </summary>
        [StringLength(50)]
        public string? Provider { get; set; } = "Zoom";

        /// <summary>
        /// Transcript URL (if available)
        /// </summary>
        public string? TranscriptUrl { get; set; }

        /// <summary>
        /// Thumbnail image URL
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        /// <summary>
        /// Processing status: Processing, Ready, Failed
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "Processing"; // Processing, Ready, Failed

        /// <summary>
        /// When recording was available
        /// </summary>
        public DateTime? AvailableAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Property
        public virtual LiveClass LiveClass { get; set; } = null!;
    }
}
