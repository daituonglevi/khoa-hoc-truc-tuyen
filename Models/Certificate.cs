using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? CertificateNumber { get; set; }

        [StringLength(500)]
        public string? CertificateUrl { get; set; }

        [NotMapped]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; } = null!;
    }
}
