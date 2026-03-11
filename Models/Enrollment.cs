using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        [Range(0, 100)]
        public double Progress { get; set; } = 0; // Sử dụng float như trong database

        [Required]
        public int Status { get; set; } = 1; // 1: Active, 2: Suspended, 3: Completed

        public DateTime? ExpiredDate { get; set; }

        // public DateTime? CompletedDate { get; set; } // Không có trong database hi�?n tại

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

        // Helper methods
        public string GetStatusText()
        {
            return Status switch
            {
                1 => "Đang học",
                2 => "Tạm dừng",
                3 => "Hoàn thành",
                _ => "Không xác đ�<nh"
            };
        }

        public bool IsExpired => ExpiredDate.HasValue && ExpiredDate.Value < DateTime.Now;
        public bool IsCompleted => Status == 3;
        public bool IsActive => Status == 1 && !IsExpired;
    }
}
