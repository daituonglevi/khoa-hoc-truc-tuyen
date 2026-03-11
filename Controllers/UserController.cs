using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;

namespace ELearningWebsite.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserController> _logger;

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: User/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            _logger.LogWarning("=== Dashboard action STARTED ===");
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Category)
                .Where(e => e.UserId == currentUser.Id)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();

            var viewModel = new UserDashboardViewModel
            {
                User = currentUser,
                TotalCourses = enrollments.Count,
                CompletedCourses = enrollments.Count(e => e.Status == 3),
                InProgressCourses = enrollments.Count(e => e.Status == 1),
                RecentEnrollments = enrollments.Take(5).ToList(),
                AverageProgress = enrollments.Any() ? enrollments.Average(e => e.Progress) : 0
            };

            return View(viewModel);
        }

        // GET: User/MyCourses
        public async Task<IActionResult> MyCourses(string? status = null, int page = 1, int pageSize = 12)
        {
            _logger.LogWarning("=== MyCourses action STARTED === status={Status}, page={Page}, pageSize={PageSize}", status, page, pageSize);
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("User not found in MyCourses");
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var query = _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Category)
                .Include(e => e.Course.Chapters)
                .Where(e => e.UserId == currentUser.Id);

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(e => e.Status == 1);
                        break;
                    case "completed":
                        query = query.Where(e => e.Status == 3);
                        break;
                    case "suspended":
                        query = query.Where(e => e.Status == 2);
                        break;
                }
            }

            var totalEnrollments = await query.CountAsync();
            _logger.LogInformation("Found {TotalEnrollments} enrollments for user {UserId}", totalEnrollments, currentUser.Id);

            var enrollments = await query
                .OrderByDescending(e => e.EnrollmentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new MyCoursesViewModel
            {
                Enrollments = enrollments,
                CurrentStatus = status,
                CurrentPage = page,
                PageSize = pageSize,
                TotalEnrollments = totalEnrollments,
                TotalPages = (int)Math.Ceiling((double)totalEnrollments / pageSize)
            };

            return View(viewModel);
        }

        // GET: User/CourseProgress/5
        public async Task<IActionResult> CourseProgress(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Category)
                .Include(e => e.Course.Chapters)
                .ThenInclude(ch => ch.Lessons)
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == currentUser.Id);

            if (enrollment == null)
            {
                return NotFound();
            }

            // Lấy danh sách LessonId thu�Tc khóa học này
            var lessonIds = enrollment.Course.Chapters
                .SelectMany(ch => ch.Lessons != null ? ch.Lessons.Select(l => l.Id) : new List<int>())
                .ToList();

            // Lấy tiến đ�T học của user cho các bài học thu�Tc khóa học này
            var lessonProgresses = await _context.LessonProgresses
                .Where(lp => lp.UserId == currentUser.Id && lessonIds.Contains(lp.LessonId))
                .ToListAsync();

            // Xác đ�<nh bài học tiếp tục học (bài đầu tiên chưa hoàn thành hoặc chưa có tiến đ�T)
            int? nextLessonId = null;
            foreach (var lessonId in lessonIds)
            {
                var progress = lessonProgresses.FirstOrDefault(lp => lp.LessonId == lessonId);
                if (progress == null || progress.ProgressPercentage < 100)
                {
                    nextLessonId = lessonId;
                    break;
                }
            }

            var viewModel = new CourseProgressViewModel
            {
                Enrollment = enrollment,
                LessonProgresses = lessonProgresses,
                NextLessonId = nextLessonId
            };

            return View(viewModel);
        }

        // POST: User/UpdateProgress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProgress(int enrollmentId, double progress)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đ�fng nhập lại!" });
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.UserId == currentUser.Id);

            if (enrollment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin đ�fng ký khóa học!" });
            }

            enrollment.Progress = progress;
            if (progress >= 100)
            {
                enrollment.Status = 3; // Completed
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật tiến đ�T thành công!" });
        }

        // GET: User/Certificate/5
        public async Task<IActionResult> Certificate(int enrollmentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Certificates)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.UserId == currentUser.Id);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khóa học hoặc bạn không có quyền truy cập";
                return RedirectToAction("MyCourses");
            }

            if (!enrollment.IsCompleted)
            {
                TempData["ErrorMessage"] = "Bạn cần hoàn thành khóa học đ�f nhận chứng ch�?!";
                return RedirectToAction("CourseProgress", new { id = enrollmentId });
            }

            // Tạo chứng ch�? nếu chưa có
            var certificate = enrollment.Certificates.FirstOrDefault();
            if (certificate == null)
            {
                certificate = new Certificate
                {
                    EnrollmentId = enrollmentId,
                    IssueDate = DateTime.Now,
                    CertificateNumber = GenerateCertificateNumber(),
                    CreatedAt = DateTime.Now
                };

                _context.Certificates.Add(certificate);
                await _context.SaveChangesAsync();
            }

            var viewModel = new CertificateViewModel
            {
                Certificate = certificate,
                Enrollment = enrollment,
                StudentName = currentUser.FullName,
                CourseName = enrollment.Course.Title ?? "Unknown Course",
                CompletionDate = DateTime.Now // Use current date since CompletedDate field doesn't exist
            };

            return View(viewModel);
        }

        private string GenerateCertificateNumber()
        {
            return $"CERT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }
    }

    // ViewModels
    public class UserDashboardViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int InProgressCourses { get; set; }
        public double AverageProgress { get; set; }
        public List<Enrollment> RecentEnrollments { get; set; } = new();
    }

    public class MyCoursesViewModel
    {
        public List<Enrollment> Enrollments { get; set; } = new();
        public string? CurrentStatus { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalPages { get; set; }
    }

    public class CourseProgressViewModel
    {
        public Enrollment Enrollment { get; set; } = null!;
        public List<LessonProgress> LessonProgresses { get; set; } = new();
        public int? NextLessonId { get; set; } // Bài học tiếp tục học
    }

    public class CertificateViewModel
    {
        public Certificate Certificate { get; set; } = null!;
        public Enrollment Enrollment { get; set; } = null!;
        public string StudentName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime CompletionDate { get; set; }
    }
}
