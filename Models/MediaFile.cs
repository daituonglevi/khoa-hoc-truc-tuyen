using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class MediaFile
    {
        public int Id { get; set; }

        [Required]
        [StringLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(400)]
        public string BlobName { get; set; } = string.Empty;

        [Required]
        [StringLength(600)]
        public string BlobPath { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string ContentType { get; set; } = "application/octet-stream";

        public long SizeBytes { get; set; }

        [Required]
        public int OwnerUserId { get; set; }

        public int? CourseId { get; set; }
        public int? FolderId { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("OwnerUserId")]
        public virtual ApplicationUser OwnerUser { get; set; } = null!;

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("FolderId")]
        public virtual MediaFolder? Folder { get; set; }
    }
}