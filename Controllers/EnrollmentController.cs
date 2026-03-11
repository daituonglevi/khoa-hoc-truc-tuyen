using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;

namespace ELearningWebsite.Controllers
{
    [Authorize]
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EnrollmentController> _logger;

        public EnrollmentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EnrollmentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Enrollment/Checkout/5
        public async Task<IActionResult> Checkout(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // Kiểm tra xem user đã đăng ký khóa học này chưa
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == id && e.UserId == currentUser.Id);

            if (existingEnrollment != null)
            {
                TempData["ErrorMessage"] = "Bạn đã đăng ký khóa học này rôi!";
                return RedirectToAction("CourseDetail", "Home", new { id = id });
            }

            var viewModel = new CheckoutViewModel
            {
                Course = course,
                OriginalPrice = course.Price,
                FinalPrice = course.Price,
                DiscountAmount = 0
            };

            return View(viewModel);
        }

        // POST: Enrollment/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel model)
        {
            try
            {
                var course = await _context.Courses.FindAsync(model.CourseId);
                if (course == null)
                {
                    TempData["ErrorMessage"] = "Khóa học không tôn tại!";
                    return RedirectToAction("Courses", "Home");
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                _logger.LogInformation("Processing payment for user: {UserId}, course: {CourseId}", currentUser.Id, model.CourseId);

                // Ki�fm tra xem user đã đ�fng ký khóa học này chưa
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == model.CourseId && e.UserId == currentUser.Id);

                if (existingEnrollment != null)
                {
                    TempData["ErrorMessage"] = "Bạn đã đăng ký khóa học này rôi!";
                    return RedirectToAction("CourseDetail", "Home", new { id = model.CourseId });
                }

                // Tạo enrollment record
                var enrollment = new Enrollment
                {
                    UserId = currentUser.Id,
                    CourseId = model.CourseId,
                    EnrollmentDate = DateTime.Now,
                    Status = 1, // Active
                    Progress = 0,
                    ExpiredDate = course.LimitDay.HasValue ? DateTime.Now.AddDays(course.LimitDay.Value) : null
                };

                _context.Enrollments.Add(enrollment);

                // Ghi log tài chính
                await LogFinanceRecord(model.FinalPrice, "Course Purchase", currentUser.UserName ?? "Unknown");

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed payment for user {UserId}, course {CourseId}", currentUser.Id, model.CourseId);

                TempData["SuccessMessage"] = "Thanh toán và đăng ký khóa học thành công!";
                return RedirectToAction("PaymentSuccess", new { enrollmentId = enrollment.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for course {CourseId}", model.CourseId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình thanh toán!";
                return RedirectToAction("Checkout", new { id = model.CourseId });
            }
        }

        // GET: Enrollment/PaymentSuccess/5
        public async Task<IActionResult> PaymentSuccess(int enrollmentId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Category)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.UserId == currentUser.Id);

            if (enrollment == null)
            {
                return NotFound();
            }

            return View(enrollment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessEnrollment(int courseId)
        {
            try
            {
                _logger.LogInformation("Starting ProcessEnrollment for courseId: {CourseId}", courseId);

                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    _logger.LogWarning("Course not found: {CourseId}", courseId);
                    return Json(new { success = false, message = "Khóa học không tôn tại!" });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    _logger.LogWarning("User not authenticated");
                    return Json(new { success = false, message = "Vui lòng đăng nhập để đăng ký khóa học!" });
                }

                _logger.LogInformation("Processing enrollment for user: {UserId}, course: {CourseId}", currentUser.Id, courseId);

                // Ki�fm tra xem user đã đ�fng ký khóa học này chưa
                var existingEnrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == courseId && e.UserId == currentUser.Id);

                if (existingEnrollment != null)
                {
                    _logger.LogWarning("User {UserId} already enrolled in course {CourseId}", currentUser.Id, courseId);
                    return Json(new { success = false, message = "Bạn đã đăng ký khóa học này rôi!" });
                }

                // Ki�fm tra khóa học có mi�.n phí không
                if (course.Price > 0)
                {
                    _logger.LogWarning("Course {CourseId} is not free. Price: {Price}", courseId, course.Price);
                    return Json(new { success = false, message = "Khóa học này không miễn phí!" });
                }

                // Tạo enrollment record
                var enrollment = new Enrollment
                {
                    UserId = currentUser.Id,
                    CourseId = courseId,
                    EnrollmentDate = DateTime.Now,
                    Status = 1, // Active
                    Progress = 0,
                    ExpiredDate = course.LimitDay.HasValue ? DateTime.Now.AddDays(course.LimitDay.Value) : null
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully enrolled user {UserId} in course {CourseId}", currentUser.Id, courseId);

                return Json(new { 
                    success = true, 
                    message = "Đ�fng ký khóa học thành công!", 
                    redirectUrl = Url.Action("MyCourses", "User") 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing free course enrollment for course {CourseId}", courseId);
                return Json(new { success = false, message = "Có l�-i xảy ra trong quá trình đ�fng ký!" });
            }
        }

        private async Task LogFinanceRecord(double amount, string description, string userName)
        {
            var now = DateTime.Now;
            var finance = new Finance
            {
                Month = now.Month,
                Year = now.Year,
                Revenue = amount,
                Fee = 0,
                Type = "Course Sale",
                Description = description,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userName,
                UpdatedBy = userName
            };

            _context.Finances.Add(finance);
        }
    }

    // ViewModels
    public class CheckoutViewModel
    {
        public Course Course { get; set; } = null!;
        public double OriginalPrice { get; set; }
        public double FinalPrice { get; set; }
        public double DiscountAmount { get; set; }
    }

    public class PaymentViewModel
    {
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public double OriginalPrice { get; set; }
        public double FinalPrice { get; set; }
        public double DiscountAmount { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = "bank_transfer";
        public string? DiscountCode { get; set; }
        public Discount? AppliedDiscount { get; set; }
    }
}
