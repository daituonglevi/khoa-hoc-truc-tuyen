using ELearningWebsite.Data;
using ELearningWebsite.Models;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ELearningWebsite.Services
{
    /// <summary>
    /// Service for managing background jobs related to live classes
    /// </summary>
    public interface ILiveClassBackgroundService
    {
        /// <summary>
        /// Schedule a job to create Zoom meeting 1 day before class starts
        /// </summary>
        void ScheduleZoomMeetingCreation(int liveClassId);

        /// <summary>
        /// Create Zoom meeting for a live class and send invitations
        /// </summary>
        Task CreateZoomMeetingAsync(int liveClassId);

        /// <summary>
        /// Send email to instructor and all enrolled students
        /// </summary>
        Task SendLiveClassInvitationsAsync(int liveClassId, string zoomJoinUrl);

        /// <summary>
        /// Send reminder email 1 hour before class starts
        /// </summary>
        Task SendClassStartReminderAsync(int liveClassId);
    }

    public class LiveClassBackgroundService : ILiveClassBackgroundService
    {
        private readonly ApplicationDbContext _context;
        private readonly IZoomService _zoomService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<LiveClassBackgroundService> _logger;

        public LiveClassBackgroundService(
            ApplicationDbContext context,
            IZoomService zoomService,
            IEmailSender emailSender,
            ILogger<LiveClassBackgroundService> logger)
        {
            _context = context;
            _zoomService = zoomService;
            _emailSender = emailSender;
            _logger = logger;
        }

        public void ScheduleZoomMeetingCreation(int liveClassId)
        {
            try
            {
                var liveClass = _context.LiveClasses
                    .Include(lc => lc.Course)
                    .FirstOrDefault(lc => lc.Id == liveClassId);

                if (liveClass == null)
                {
                    _logger.LogWarning($"LiveClass {liveClassId} not found for scheduling");
                    return;
                }

                // Schedule job to run 1 day before the class starts
                var jobId = BackgroundJob.Schedule<ILiveClassBackgroundService>(
                    service => service.CreateZoomMeetingAsync(liveClassId),
                    liveClass.ScheduledDateTime.AddDays(-1)
                );

                _logger.LogInformation($"Zoom meeting creation job scheduled for LiveClass {liveClassId}: {jobId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error scheduling Zoom meeting creation: {ex.Message}");
            }
        }

        public async Task CreateZoomMeetingAsync(int liveClassId)
        {
            try
            {
                var liveClass = await _context.LiveClasses
                    .Include(lc => lc.Course)
                    .Include(lc => lc.Instructor)
                    .FirstOrDefaultAsync(lc => lc.Id == liveClassId);

                if (liveClass == null)
                {
                    _logger.LogWarning($"LiveClass {liveClassId} not found");
                    return;
                }

                // Skip if Zoom meeting already created
                if (!string.IsNullOrEmpty(liveClass.ZoomMeetingId))
                {
                    _logger.LogInformation($"Zoom meeting already created for LiveClass {liveClassId}");
                    return;
                }

                // Create Zoom meeting
                var zoomRequest = new ZoomMeetingRequest
                {
                    Topic = liveClass.Title,
                    Description = liveClass.Description ?? "",
                    StartTime = liveClass.ScheduledDateTime,
                    DurationMinutes = liveClass.DurationMinutes ?? 60,
                    RecordingEnabled = liveClass.IsRecordingEnabled,
                    Password = GenerateRandomPassword(6)
                };

                var zoomResponse = await _zoomService.CreateMeetingAsync(zoomRequest);

                if (zoomResponse == null)
                {
                    _logger.LogError($"Failed to create Zoom meeting for LiveClass {liveClassId}");
                    return;
                }

                // Save Zoom meeting details
                liveClass.ZoomMeetingId = zoomResponse.MeetingId;
                liveClass.JoinUrl = zoomResponse.JoinUrl;
                liveClass.StartUrl = zoomResponse.StartUrl;
                liveClass.UpdatedAt = DateTime.UtcNow;

                _context.LiveClasses.Update(liveClass);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Zoom meeting created: {zoomResponse.MeetingId} for LiveClass {liveClassId}");

                // Send invitations to instructor and students
                await SendLiveClassInvitationsAsync(liveClassId, zoomResponse.JoinUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating Zoom meeting for LiveClass {liveClassId}: {ex.Message}");
            }
        }

        public async Task SendLiveClassInvitationsAsync(int liveClassId, string zoomJoinUrl)
        {
            try
            {
                var liveClass = await _context.LiveClasses
                    .Include(lc => lc.Course)
                    .Include(lc => lc.Instructor)
                    .FirstOrDefaultAsync(lc => lc.Id == liveClassId);

                if (liveClass == null)
                {
                    _logger.LogWarning($"LiveClass {liveClassId} not found");
                    return;
                }

                // Get all students enrolled in the course
                var enrolledStudents = await _context.Enrollments
                    .Where(e => e.CourseId == liveClass.CourseId && e.Status != 2) // 2 = Suspended
                    .Include(e => e.User)
                    .Select(e => e.User)
                    .ToListAsync();

                // Email to instructor
                var instructorEmailSubject = $"Lớp học trực tuyến sắp diễn ra: {liveClass.Title}";
                var instructorEmailBody = BuildInstructorEmailBody(liveClass, zoomJoinUrl);
                await _emailSender.SendEmailAsync(
                    liveClass.Instructor.Email ?? "",
                    instructorEmailSubject,
                    instructorEmailBody);

                _logger.LogInformation($"Invitation email sent to instructor for LiveClass {liveClassId}");

                // Email to students
                var studentEmailSubject = $"Mời tham gia lớp học trực tuyến: {liveClass.Title}";
                foreach (var student in enrolledStudents)
                {
                    if (string.IsNullOrEmpty(student.Email)) continue;

                    var studentEmailBody = BuildStudentEmailBody(liveClass, zoomJoinUrl, student.FullName ?? student.UserName ?? "");
                    await _emailSender.SendEmailAsync(
                        student.Email,
                        studentEmailSubject,
                        studentEmailBody);
                }

                _logger.LogInformation($"Invitation emails sent to {enrolledStudents.Count} students for LiveClass {liveClassId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending invitations for LiveClass {liveClassId}: {ex.Message}");
            }
        }

        public async Task SendClassStartReminderAsync(int liveClassId)
        {
            try
            {
                var liveClass = await _context.LiveClasses
                    .Include(lc => lc.Course)
                    .Include(lc => lc.Instructor)
                    .FirstOrDefaultAsync(lc => lc.Id == liveClassId);

                if (liveClass == null || string.IsNullOrEmpty(liveClass.JoinUrl))
                {
                    return;
                }

                // Get enrolled students
                var enrolledStudents = await _context.Enrollments
                    .Where(e => e.CourseId == liveClass.CourseId && e.Status != 2)
                    .Include(e => e.User)
                    .Select(e => e.User)
                    .ToListAsync();

                // Send reminder emails
                var subject = $"🔴 LIVE: Lớp học {liveClass.Title} bắt đầu trong 1 giờ!";
                foreach (var student in enrolledStudents)
                {
                    if (string.IsNullOrEmpty(student.Email)) continue;

                    var body = $@"
<h2>Lớp học trực tuyến bắt đầu trong 1 giờ!</h2>
<p>Xin chào {student.FullName ?? student.UserName},</p>
<p>Lớp học <strong>{liveClass.Title}</strong> sẽ bắt đầu lúc <strong>{liveClass.ScheduledDateTime:dd/MM/yyyy HH:mm}</strong></p>
<p><a href='{liveClass.JoinUrl}' style='display:inline-block;padding:10px 20px;background-color:#0066cc;color:white;text-decoration:none;border-radius:5px;'>
  Tham gia lớp học ngay
</a></p>
<p>Giáo viên: {liveClass.Instructor.FullName ?? liveClass.Instructor.UserName}</p>
";
                    await _emailSender.SendEmailAsync(student.Email, subject, body);
                }

                _logger.LogInformation($"Reminder emails sent for LiveClass {liveClassId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending reminder for LiveClass {liveClassId}: {ex.Message}");
            }
        }

        private string BuildInstructorEmailBody(LiveClass liveClass, string zoomJoinUrl)
        {
            return $@"
<h2>Thông báo Lớp Học Trực Tuyến</h2>
<p>Xin chào {liveClass.Instructor.FullName},</p>
<p>Lớp học của bạn <strong>{liveClass.Title}</strong> đã sẵn sàng!</p>
<h3>Chi Tiết Lớp Học:</h3>
<ul>
  <li><strong>Tên khóa học:</strong> {liveClass.Course.Title}</li>
  <li><strong>Tên bài học:</strong> {liveClass.Title}</li>
  <li><strong>Thời gian:</strong> {liveClass.ScheduledDateTime:dd/MM/yyyy HH:mm}</li>
  <li><strong>Thời lượng:</strong> {liveClass.DurationMinutes} phút</li>
  <li><strong>Số lượng học viên:</strong> (sẽ cập nhật khi bắt đầu)</li>
</ul>
<h3>Tham Gia Lớp Học:</h3>
<p><a href='{zoomJoinUrl}' style='display:inline-block;padding:12px 24px;background-color:#ff6b6b;color:white;text-decoration:none;border-radius:5px;font-size:16px;'>
  Bắt Đầu Giảng Dạy
</a></p>
<h3>Lưu Ý:</h3>
<ul>
  <li>✅ Bài giảng sẽ được ghi hình tự động</li>
  <li>✅ Học viên sẽ được điểm danh tự động</li>
  <li>✅ Bài ghi hình sẽ có sẵn sau 2 giờ kết thúc lớp</li>
</ul>
";
        }

        private string BuildStudentEmailBody(LiveClass liveClass, string zoomJoinUrl, string studentName)
        {
            return $@"
<h2>Lời Mời Tham Gia Lớp Học Trực Tuyến</h2>
<p>Xin chào {studentName},</p>
<p>Bạn được mời tham gia lớp học trực tuyến <strong>{liveClass.Title}</strong></p>
<h3>Chi Tiết Lớp Học:</h3>
<ul>
  <li><strong>Khóa học:</strong> {liveClass.Course.Title}</li>
  <li><strong>Bài giảng:</strong> {liveClass.Title}</li>
  <li><strong>Ngày giờ:</strong> {liveClass.ScheduledDateTime:dd/MM/yyyy HH:mm}</li>
  <li><strong>Thời lượng:</strong> {liveClass.DurationMinutes} phút</li>
  <li><strong>Giáo viên:</strong> {liveClass.Instructor.FullName ?? liveClass.Instructor.UserName}</li>
</ul>
<h3>Tham Gia Ngay:</h3>
<p><a href='{zoomJoinUrl}' style='display:inline-block;padding:12px 24px;background-color:#51cf66;color:white;text-decoration:none;border-radius:5px;font-size:16px;'>
  Tham Gia Lớp Học
</a></p>
<h3>Thông Tin Quan Trọng:</h3>
<ul>
  <li>✅ Bạn sẽ được điểm danh tự động khi tham gia</li>
  <li>✅ Nếu bạn vắng mặt, bạn có thể xem bài ghi hình sau</li>
  <li>✅ Mời chúng bạn 5 phút trước giờ bắt đầu</li>
</ul>
";
        }

        private string GenerateRandomPassword(int length)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, length)
                .Select(_ => validChars[random.Next(validChars.Length)])
                .ToArray());
        }
    }
}
