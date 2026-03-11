using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public int LessonId { get; set; }

        public int? UserId { get; set; }

        public int? ParentCommentId { get; set; }

        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDelete { get; set; } = false;

        // Navigation properties
        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
