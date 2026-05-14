using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class CourseCollaborator
    {
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int GrantedByUserId { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("GrantedByUserId")]
        public virtual ApplicationUser GrantedByUser { get; set; } = null!;
    }
}