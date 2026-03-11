using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using ELearningWebsite.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CertificatesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CertificatesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Certificates
        public async Task<IActionResult> Index(int page = 1, string searchTerm = "", int? courseId = null,
            int? userId = null, string status = "", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var viewModel = new CertificateIndexViewModel
            {
                CurrentPage = page,
                SearchTerm = searchTerm,
                CourseId = courseId,
                UserId = userId,
                Status = status,
                FromDate = fromDate,
                ToDate = toDate
            };

            // Build query
            var query = _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.CertificateNumber.Contains(searchTerm) ||
                                        c.Enrollment.Course.Title.Contains(searchTerm));
            }

            // Apply course filter
            if (courseId.HasValue)
            {
                query = query.Where(c => c.Enrollment.CourseId == courseId.Value);
            }

            // Apply user filter
            if (userId.HasValue)
            {
                query = query.Where(c => c.Enrollment.UserId == userId.Value);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "issued":
                        query = query.Where(c => !string.IsNullOrEmpty(c.CertificateUrl));
                        break;
                    case "pending":
                        query = query.Where(c => string.IsNullOrEmpty(c.CertificateUrl));
                        break;
                    // "all" - no filter
                }
            }

            // Apply date filters
            if (fromDate.HasValue)
            {
                query = query.Where(c => c.IssueDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(c => c.IssueDate.Date <= toDate.Value.Date);
            }

            // Get total count
            viewModel.TotalItems = await query.CountAsync();
            viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalItems / viewModel.PageSize);

            // Get paginated results
            var certificates = await query
                .OrderByDescending(c => c.IssueDate)
                .Skip((page - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .Select(c => new CertificateListItem
                {
                    Id = c.Id,
                    EnrollmentId = c.EnrollmentId,
                    UserId = c.Enrollment.UserId,
                    UserName = "User " + c.Enrollment.UserId, // Simplified since we don't have User table
                    UserEmail = "user" + c.Enrollment.UserId + "@example.com",
                    CourseId = c.Enrollment.CourseId,
                    CourseTitle = c.Enrollment.Course.Title ?? "Unknown Course",
                    CertificateNumber = c.CertificateNumber ?? "",
                    CertificateUrl = c.CertificateUrl,
                    IssueDate = c.IssueDate,
                    EnrollmentDate = c.Enrollment.EnrollmentDate,
                    Progress = c.Enrollment.Progress,
                    EnrollmentStatus = c.Enrollment.Status,
                    EnrollmentStatusText = c.Enrollment.GetStatusText()
                })
                .ToListAsync();

            viewModel.Certificates = certificates;

            // Load filter options
            await LoadFilterOptions(viewModel);

            return View(viewModel);
        }

        // GET: Admin/Certificates/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chứng ch�?";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CertificateDetailsViewModel
            {
                Id = certificate.Id,
                EnrollmentId = certificate.EnrollmentId,
                UserId = certificate.Enrollment.UserId,
                UserName = certificate.Enrollment.User?.UserName ?? "Unknown User",
                UserEmail = certificate.Enrollment.User?.Email ?? "No Email",
                UserPhone = certificate.Enrollment.User?.PhoneNumber ?? "No Phone",
                CourseId = certificate.Enrollment.CourseId,
                CourseTitle = certificate.Enrollment.Course.Title ?? "Unknown Course",
                CourseDescription = certificate.Enrollment.Course.Description ?? "",
                CoursePrice = certificate.Enrollment.Course.Price,
                CertificateNumber = certificate.CertificateNumber ?? "",
                CertificateUrl = certificate.CertificateUrl,
                IssueDate = certificate.IssueDate,
                EnrollmentDate = certificate.Enrollment.EnrollmentDate,
                CompletionDate = certificate.Enrollment.Status == 3 ? certificate.IssueDate : null,
                Progress = certificate.Enrollment.Progress,
                EnrollmentStatus = certificate.Enrollment.Status,
                EnrollmentStatusText = certificate.Enrollment.GetStatusText()
            };

            return View(viewModel);
        }

        // GET: Admin/Certificates/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CertificateCreateViewModel();
            await LoadEnrollmentOptions(viewModel);
            return View(viewModel);
        }

        // POST: Admin/Certificates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CertificateCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if enrollment exists and is completed
                    var enrollment = await _context.Enrollments
                        .Include(e => e.Course)
                        .FirstOrDefaultAsync(e => e.Id == model.EnrollmentId);

                    if (enrollment == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy đ�fng ký khóa học";
                        await LoadEnrollmentOptions(model);
                        return View(model);
                    }

                    // Check if certificate already exists
                    var existingCertificate = await _context.Certificates
                        .FirstOrDefaultAsync(c => c.EnrollmentId == model.EnrollmentId);

                    if (existingCertificate != null)
                    {
                        TempData["ErrorMessage"] = "Chứng ch�? cho đ�fng ký này đã tôn tại";
                        await LoadEnrollmentOptions(model);
                        return View(model);
                    }

                    var certificate = new Certificate
                    {
                        EnrollmentId = model.EnrollmentId,
                        CertificateNumber = model.CertificateNumber,
                        IssueDate = model.IssueDate,
                        CertificateUrl = model.CertificateUrl
                    };

                    _context.Certificates.Add(certificate);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Tạo chứng chỉ thành công";
                    return RedirectToAction(nameof(Details), new { id = certificate.Id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }

            await LoadEnrollmentOptions(model);
            return View(model);
        }

        // GET: Admin/Certificates/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chứng chỉ";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CertificateEditViewModel
            {
                Id = certificate.Id,
                EnrollmentId = certificate.EnrollmentId,
                CertificateNumber = certificate.CertificateNumber ?? "",
                IssueDate = certificate.IssueDate,
                CertificateUrl = certificate.CertificateUrl,
                UserName = certificate.Enrollment.User?.UserName ?? "Unknown User",
                CourseTitle = certificate.Enrollment.Course.Title ?? "Unknown Course",
                Progress = certificate.Enrollment.Progress,
                Status = certificate.Enrollment.Status,
                EnrollmentDate = certificate.Enrollment.EnrollmentDate
            };

            return View(viewModel);
        }

        // POST: Admin/Certificates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CertificateEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var certificate = await _context.Certificates.FindAsync(id);
                    if (certificate == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy chứng chỉ";
                        return RedirectToAction(nameof(Index));
                    }

                    certificate.CertificateNumber = model.CertificateNumber;
                    certificate.IssueDate = model.IssueDate;
                    certificate.CertificateUrl = model.CertificateUrl;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật chứng chỉ thành công";
                    return RedirectToAction(nameof(Details), new { id = certificate.Id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                }
            }

            // Reload data if validation fails
            var originalCertificate = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (originalCertificate != null)
            {
                model.UserName = originalCertificate.Enrollment.User?.UserName ?? "Unknown User";
                model.CourseTitle = originalCertificate.Enrollment.Course.Title ?? "Unknown Course";
                model.Progress = originalCertificate.Enrollment.Progress;
                model.Status = originalCertificate.Enrollment.Status;
                model.EnrollmentDate = originalCertificate.Enrollment.EnrollmentDate;
            }

            return View(model);
        }

        // GET: Admin/Certificates/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chứng ch�?";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CertificateDeleteViewModel
            {
                Id = certificate.Id,
                CertificateNumber = certificate.CertificateNumber ?? "",
                UserName = certificate.Enrollment.User?.UserName ?? "Unknown User",
                CourseTitle = certificate.Enrollment.Course.Title ?? "Unknown Course",
                IssueDate = certificate.IssueDate,
                HasCertificateFile = !string.IsNullOrEmpty(certificate.CertificateUrl),
                CertificateUrl = certificate.CertificateUrl
            };

            return View(viewModel);
        }

        // POST: Admin/Certificates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var certificate = await _context.Certificates.FindAsync(id);
                if (certificate == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy chứng ch�?";
                    return RedirectToAction(nameof(Index));
                }

                _context.Certificates.Remove(certificate);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa chứng ch�? thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task LoadFilterOptions(CertificateIndexViewModel viewModel)
        {
            // Load available courses
            var courses = await _context.Courses
                .Where(c => c.Status == "Published")
                .OrderBy(c => c.Title)
                .Select(c => new CourseOption
                {
                    Id = c.Id,
                    Title = c.Title ?? "Unknown Course",
                    Price = c.Price
                })
                .ToListAsync();

            viewModel.AvailableCourses = courses;

            // Load available users (simplified)
            var userIds = await _context.Enrollments
                .Select(e => e.UserId)
                .Distinct()
                .OrderBy(id => id)
                .ToListAsync();

            viewModel.AvailableUsers = userIds.Select(id => new UserOption
            {
                Id = id,
                UserName = _userManager.FindByIdAsync(id.ToString()).Result?.UserName ?? "Unknown User",
                Email = _userManager.FindByIdAsync(id.ToString()).Result?.Email ?? "No Email"
            }).ToList();
        }

        // GET: Admin/Certificates/Generate
        public async Task<IActionResult> Generate()
        {
            var viewModel = new CertificateGenerateViewModel();
            await LoadEnrollmentOptionsForGenerate(viewModel);
            return View(viewModel);
        }

        // POST: Admin/Certificates/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(CertificateGenerateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var enrollment = await _context.Enrollments
                        .Include(e => e.Course)
                        .FirstOrDefaultAsync(e => e.Id == model.EnrollmentId);

                    if (enrollment == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy đ�fng ký khóa học";
                        await LoadEnrollmentOptionsForGenerate(model);
                        return View(model);
                    }

                    // Check if certificate already exists
                    var existingCertificate = await _context.Certificates
                        .FirstOrDefaultAsync(c => c.EnrollmentId == model.EnrollmentId);

                    if (existingCertificate != null)
                    {
                        TempData["ErrorMessage"] = "Chứng ch�? cho đ�fng ký này đã tôn tại";
                        await LoadEnrollmentOptionsForGenerate(model);
                        return View(model);
                    }

                    // Generate certificate number
                    var certificateNumber = GenerateCertificateNumber();

                    var certificate = new Certificate
                    {
                        EnrollmentId = model.EnrollmentId,
                        CertificateNumber = certificateNumber,
                        IssueDate = DateTime.Now,
                        CertificateUrl = null // Will be generated later
                    };

                    _context.Certificates.Add(certificate);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Tạo chứng ch�? thành công v�>i sđ: " + certificateNumber;
                    return RedirectToAction(nameof(Details), new { id = certificate.Id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                }
            }

            await LoadEnrollmentOptionsForGenerate(model);
            return View(model);
        }

        // GET: Admin/Certificates/Statistics
        public async Task<IActionResult> Statistics()
        {
            var now = DateTime.Now;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var thisYear = new DateTime(now.Year, 1, 1);

            var stats = new CertificateStatisticsViewModel
            {
                TotalCertificates = await _context.Certificates.CountAsync(),
                CertificatesThisMonth = await _context.Certificates
                    .CountAsync(c => c.IssueDate >= thisMonth),
                CertificatesThisYear = await _context.Certificates
                    .CountAsync(c => c.IssueDate >= thisYear),
                CertificatesWithFiles = await _context.Certificates
                    .CountAsync(c => !string.IsNullOrEmpty(c.CertificateUrl)),
                CertificatesWithoutFiles = await _context.Certificates
                    .CountAsync(c => string.IsNullOrEmpty(c.CertificateUrl)),
                CompletedEnrollments = await _context.Enrollments.CountAsync(e => e.Status == 3),
                PendingCertificates = await _context.Enrollments
                    .CountAsync(e => e.Status == 3 && !_context.Certificates.Any(c => c.EnrollmentId == e.Id))
            };

            // Calculate completion rate
            var totalEnrollments = await _context.Enrollments.CountAsync();
            stats.CertificateCompletionRate = totalEnrollments > 0 ?
                (double)stats.CompletedEnrollments / totalEnrollments * 100 : 0;

            // Get trend data for last 30 days
            var trendData = new List<CertificateTrendData>();
            for (int i = 29; i >= 0; i--)
            {
                var date = DateTime.Now.Date.AddDays(-i);
                var count = await _context.Certificates
                    .CountAsync(c => c.IssueDate.Date == date);
                trendData.Add(new CertificateTrendData { Date = date, Count = count });
            }
            stats.TrendData = trendData;

            // Get top courses by certificate count
            var topCourses = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .GroupBy(c => new { c.Enrollment.CourseId, c.Enrollment.Course.Title })
                .Select(g => new TopCourseData
                {
                    CourseId = g.Key.CourseId,
                    CourseTitle = g.Key.Title ?? "Unknown Course",
                    CertificateCount = g.Count(),
                    CompletionRate = 0 // Will calculate separately
                })
                .OrderByDescending(x => x.CertificateCount)
                .Take(10)
                .ToListAsync();

            // Calculate completion rates for top courses
            foreach (var course in topCourses)
            {
                var totalEnrollmentsForCourse = await _context.Enrollments
                    .CountAsync(e => e.CourseId == course.CourseId);
                course.CompletionRate = totalEnrollmentsForCourse > 0 ?
                    (double)course.CertificateCount / totalEnrollmentsForCourse * 100 : 0;
            }

            stats.TopCourses = topCourses;

            // Get recent certificates
            var recentCertificates = await _context.Certificates
                .Include(c => c.Enrollment)
                .ThenInclude(e => e.Course)
                .Include(c => c.Enrollment.User)
                .OrderByDescending(c => c.IssueDate)
                .Take(10)
                .Select(c => new RecentCertificateData
                {
                    Id = c.Id,
                    CertificateNumber = c.CertificateNumber ?? "",
                    UserName = (c.Enrollment.User != null ? c.Enrollment.User.UserName : "Unknown User"),
                    CourseTitle = c.Enrollment.Course.Title ?? "Unknown Course",
                    IssueDate = c.IssueDate
                })
                .ToListAsync();

            stats.RecentCertificates = recentCertificates;

            return View(stats);
        }

        // POST: Admin/Certificates/RegenerateCertificate/5
        [HttpPost]
        public async Task<IActionResult> RegenerateCertificate(int id)
        {
            try
            {
                var certificate = await _context.Certificates.FindAsync(id);
                if (certificate == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy chứng ch�?" });
                }

                // Generate new certificate number
                certificate.CertificateNumber = GenerateCertificateNumber();
                certificate.IssueDate = DateTime.Now;
                certificate.CertificateUrl = null; // Reset URL to regenerate

                await _context.SaveChangesAsync();

                return Json(new {
                    success = true,
                    message = "Tạo lại chứng chỉ thành công",
                    certificateNumber = certificate.CertificateNumber
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private async Task LoadEnrollmentOptions(CertificateCreateViewModel viewModel)
        {
            var completedEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.User)
                .Where(e => e.Status == 3 && !_context.Certificates.Any(c => c.EnrollmentId == e.Id))
                .OrderByDescending(e => e.EnrollmentDate)
                .Select(e => new EnrollmentOption
                {
                    Id = e.Id,
                    UserName = e.User.UserName ?? "Unknown User",
                    CourseTitle = e.Course.Title ?? "Unknown Course",
                    EnrollmentDate = e.EnrollmentDate,
                    Progress = e.Progress
                })
                .ToListAsync();

            viewModel.AvailableEnrollments = completedEnrollments;
        }

        private async Task LoadEnrollmentOptionsForGenerate(CertificateGenerateViewModel viewModel)
        {
            var completedEnrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.User)
                .Where(e => e.Status == 3 && !_context.Certificates.Any(c => c.EnrollmentId == e.Id))
                .OrderByDescending(e => e.EnrollmentDate)
                .Select(e => new EnrollmentOption
                {
                    Id = e.Id,
                    UserName = e.User.UserName ?? "Unknown User",
                    CourseTitle = e.Course.Title ?? "Unknown Course",
                    EnrollmentDate = e.EnrollmentDate,
                    Progress = e.Progress
                })
                .ToListAsync();

            viewModel.AvailableEnrollments = completedEnrollments;
        }

        private string GenerateCertificateNumber()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
