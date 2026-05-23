using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    /// <summary>
    /// Live Class entity - represents a scheduled live streaming session via Zoom/Meet
    /// </summary>
    public class LiveClass
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Course this live class belongs to
        /// </summary>
        [Required]
        [ForeignKey("Course")]
        public int CourseId { get; set; }

        /// <summary>
        /// Lesson this live class is associated with (optional, can be standalone)
        /// </summary>
        [ForeignKey("Lesson")]
        public int? LessonId { get; set; }

        /// <summary>
        /// Title of the live class
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Description/agenda for the class
        /// </summary>
        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// When the class is scheduled
        /// </summary>
        [Required]
        public DateTime ScheduledDateTime { get; set; }

        /// <summary>
        /// Duration in minutes
        /// </summary>
        public int? DurationMinutes { get; set; }

        /// <summary>
        /// Maximum number of participants allowed (null = unlimited)
        /// </summary>
        public int? MaxParticipants { get; set; }

        /// <summary>
        /// Zoom meeting ID returned from Zoom API
        /// </summary>
        [StringLength(100)]
        public string? ZoomMeetingId { get; set; }

        /// <summary>
        /// Join URL from Zoom (for students)
        /// </summary>
        public string? JoinUrl { get; set; }

        /// <summary>
        /// Instructor join URL from Zoom (with presenter controls)
        /// </summary>
        public string? StartUrl { get; set; }

        /// <summary>
        /// RTMP or HLS stream URL for embedding in web player
        /// </summary>
        public string? StreamUrl { get; set; }

        /// <summary>
        /// URL to the recorded video (from Zoom cloud or our storage)
        /// </summary>
        public string? RecordingUrl { get; set; }

        /// <summary>
        /// Duration of recording in seconds (if available)
        /// </summary>
        public long? RecordingDurationSeconds { get; set; }

        /// <summary>
        /// Status: Scheduled, Live, Completed, Cancelled
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Scheduled"; // Scheduled, Live, Completed, Cancelled

        /// <summary>
        /// Actual start time (when instructor clicked "Go Live")
        /// </summary>
        public DateTime? ActualStartTime { get; set; }

        /// <summary>
        /// Actual end time (when class ended)
        /// </summary>
        public DateTime? ActualEndTime { get; set; }

        /// <summary>
        /// Is recording enabled for this class?
        /// </summary>
        public bool IsRecordingEnabled { get; set; } = true;

        /// <summary>
        /// Is recording publicly available to all students?
        /// </summary>
        public bool IsRecordingPublic { get; set; } = true;

        /// <summary>
        /// Instructor ID who created this class
        /// </summary>
        [Required]
        public int CreateBy { get; set; }

        /// <summary>
        /// Additional metadata (JSON) - can store custom Zoom settings, etc
        /// </summary>
        public string? MetaData { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Course Course { get; set; } = null!;
        public virtual Lesson? Lesson { get; set; }
        public virtual ApplicationUser Instructor { get; set; } = null!;
        public virtual ICollection<LiveClassAttendance> Attendances { get; set; } = new List<LiveClassAttendance>();
        public virtual ICollection<LiveClassRecording> Recordings { get; set; } = new List<LiveClassRecording>();
    }
}
