using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    /// <summary>
    /// Attendance tracking for live classes
    /// </summary>
    public class LiveClassAttendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("LiveClass")]
        public int LiveClassId { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        /// <summary>
        /// When student joined the class
        /// </summary>
        public DateTime? JoinedAt { get; set; }

        /// <summary>
        /// When student left the class
        /// </summary>
        public DateTime? LeftAt { get; set; }

        /// <summary>
        /// Total attendance duration in minutes
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Status: Attended, Absent, Excused, Pending
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Attended, Absent, Excused, Pending

        /// <summary>
        /// IP address of the attendee (for security audit)
        /// </summary>
        [StringLength(50)]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Device info (for tracking simultaneous logins)
        /// </summary>
        [StringLength(200)]
        public string? DeviceInfo { get; set; }

        /// <summary>
        /// Notes from instructor (if marking absent with reason, etc)
        /// </summary>
        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual LiveClass LiveClass { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
