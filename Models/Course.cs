using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Course
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public double Price { get; set; } // Sử dụng double như trong database

        [Required]
        public string Thumbnail { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public int? Duration { get; set; } // Duration in minutes

        [Required]
        public string Status { get; set; } = "Draft"; // Draft, Published, Pending

        [Required]
        public string PreviewVideo { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public int? LimitDay { get; set; }

        [Required]
        public int CreateBy { get; set; } // Sử dụng int như trong database

        public int? UpdateBy { get; set; }

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Discount> Discounts { get; set; } = new List<Discount>();
    }
}
