using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ELearningWebsite.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Avatar { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        public bool IsVerified { get; set; } = false;

        public string? VerificationToken { get; set; }

        // Navigation properties
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
