using System.ComponentModel.DataAnnotations;

namespace ELearningWebsite.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int Status { get; set; } = 1; // 1: Active, 0: Inactive

        // Tạm thời comment vì database không có các c�Tt này
        // public DateTime CreatedAt { get; set; } = DateTime.Now;
        // public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
