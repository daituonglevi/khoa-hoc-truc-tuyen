using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class MediaFolder
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        public int? ParentFolderId { get; set; }

        [Required]
        public int OwnerUserId { get; set; }

        public int? CourseId { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("ParentFolderId")]
        public virtual MediaFolder? ParentFolder { get; set; }

        [ForeignKey("OwnerUserId")]
        public virtual ApplicationUser OwnerUser { get; set; } = null!;

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        public virtual ICollection<MediaFolder> Children { get; set; } = new List<MediaFolder>();
        public virtual ICollection<MediaFile> Files { get; set; } = new List<MediaFile>();
    }
}