using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        public int ChapterId { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Content { get; set; }

        [StringLength(500)]
        public string? VideoUrl { get; set; }

        public int? Duration { get; set; }

        [Required]
        public int OrderIndex { get; set; }

        /// <summary>
        /// Type of lesson: Video, Text, Quiz, LiveClass, Assignment
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "Video"; // Video, Text, Quiz, LiveClass, Assignment

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// For LiveClass type lessons - reference to the live class event
        /// </summary>
        [ForeignKey("LiveClass")]
        public int? LiveClassId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public int CreateBy { get; set; }

        public int? UpdateBy { get; set; }

        // Navigation properties
        [ForeignKey("ChapterId")]
        public virtual Chapter? Chapter { get; set; }

        public virtual Quiz? Quiz { get; set; }

        public virtual LiveClass? LiveClass { get; set; }

        public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    }
} 