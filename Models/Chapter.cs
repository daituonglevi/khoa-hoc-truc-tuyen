using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Chapter
    {
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public int CreateBy { get; set; }

        public int? UpdateBy { get; set; }

        // Navigation properties
        public virtual Course? Course { get; set; }
        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
