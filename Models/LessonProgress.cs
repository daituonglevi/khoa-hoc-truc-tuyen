using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class LessonProgress
    {
        public int Id { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Range(0, 100)]
        public float ProgressPercentage { get; set; } = 0;

        public float? TimeSpent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public string? Status { get; set; }

        public int? Passing { get; set; }

        public int? CountDoing { get; set; }

        public float? HighestMark { get; set; }

        // Navigation properties
        [ForeignKey("LessonId")]
        public virtual Lesson? Lesson { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
}
