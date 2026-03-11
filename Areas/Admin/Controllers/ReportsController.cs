using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using ELearningWebsite.Services;
using ELearningWebsite.Areas.Admin.ViewModels;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportExportService _reportExportService;

        public ReportsController(ApplicationDbContext context, IReportExportService reportExportService)
        {
            _context = context;
            _reportExportService = reportExportService;
        }

        // GET: Admin/Reports
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new ReportsIndexViewModel();

                // Overall Statistics
                viewModel.TotalUsers = await _context.Users.CountAsync();
                viewModel.TotalCourses = await _context.Courses.CountAsync();
                viewModel.TotalEnrollments = await _context.Enrollments.CountAsync();
                viewModel.TotalCategories = await _context.Categories.CountAsync();

                // Revenue Statistics
                viewModel.TotalRevenue = await _context.Finances.SumAsync(f => f.Revenue);
                viewModel.TotalFees = await _context.Finances.SumAsync(f => f.Fee);
                viewModel.NetRevenue = viewModel.TotalRevenue - viewModel.TotalFees;

                // Course Statistics
                viewModel.PublishedCourses = await _context.Courses.CountAsync(c => c.Status == "Published");
                viewModel.DraftCourses = await _context.Courses.CountAsync(c => c.Status == "Draft");
                viewModel.FreeCourses = await _context.Courses.CountAsync(c => c.Price == 0);
                viewModel.PaidCourses = await _context.Courses.CountAsync(c => c.Price > 0);

                // User Statistics
                viewModel.VerifiedUsers = await _context.Users.CountAsync(u => u.IsVerified);
                viewModel.UnverifiedUsers = await _context.Users.CountAsync(u => !u.IsVerified);

                // Enrollment Statistics
                viewModel.ActiveEnrollments = await _context.Enrollments.CountAsync(e => e.Status == 1);
                viewModel.CompletedEnrollments = await _context.Enrollments.CountAsync(e => e.Status == 3);
                viewModel.SuspendedEnrollments = await _context.Enrollments.CountAsync(e => e.Status == 2);

                // Monthly Trends (last 6 months)
                var monthlyTrends = new List<MonthlyTrend>();
                for (int i = 5; i >= 0; i--)
                {
                    var month = DateTime.Now.AddMonths(-i);
                    var startOfMonth = new DateTime(month.Year, month.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    var newUsers = await _context.Users
                        .CountAsync(u => u.CreatedAt >= startOfMonth && u.CreatedAt <= endOfMonth);

                    var newCourses = await _context.Courses
                        .CountAsync(c => c.CreatedAt >= startOfMonth && c.CreatedAt <= endOfMonth);

                    var newEnrollments = await _context.Enrollments
                        .CountAsync(e => e.EnrollmentDate >= startOfMonth && e.EnrollmentDate <= endOfMonth);

                    var monthlyRevenue = await _context.Finances
                        .Where(f => f.Month == month.Month && f.Year == month.Year)
                        .SumAsync(f => f.Revenue);

                    monthlyTrends.Add(new MonthlyTrend
                    {
                        Month = month.ToString("MM/yyyy"),
                        Users = newUsers,
                        Courses = newCourses,
                        Enrollments = newEnrollments,
                        Revenue = monthlyRevenue
                    });
                }
                viewModel.MonthlyTrends = monthlyTrends;

                // Recent Activities (mock data for now)
                viewModel.RecentActivities = new List<RecentActivity>
                {
                    new RecentActivity
                    {
                        Type = "User Registration",
                        Description = "Người dùng m�>i đ�fng ký",
                        CreatedAt = DateTime.Now.AddHours(-2),
                        UserName = "System"
                    },
                    new RecentActivity
                    {
                        Type = "Course Published",
                        Description = "Khóa học m�>i được xuất bản",
                        CreatedAt = DateTime.Now.AddHours(-5),
                        UserName = "Admin"
                    }
                };

                // Top Categories by Courses
                viewModel.TopCategories = await _context.Categories
                    .Include(c => c.Courses)
                    .Select(c => new CategoryReport
                    {
                        CategoryName = c.Name,
                        CourseCount = c.Courses.Count(),
                        PublishedCourses = c.Courses.Count(course => course.Status == "Published"),
                        TotalEnrollments = c.Courses.SelectMany(course => course.Enrollments).Count()
                    })
                    .OrderByDescending(c => c.CourseCount)
                    .Take(10)
                    .ToListAsync();

                // Top Courses by Enrollments
                viewModel.TopCourses = await _context.Courses
                    .Include(c => c.Enrollments)
                    .Include(c => c.Category)
                    .Select(c => new CourseReport
                    {
                        CourseName = c.Title ?? "Untitled",
                        CategoryName = c.Category != null ? c.Category.Name : "Uncategorized",
                        EnrollmentCount = c.Enrollments.Count(),
                        Price = (decimal)c.Price,
                        IsPublished = c.Status == "Published",
                        CreatedAt = c.CreatedAt
                    })
                    .OrderByDescending(c => c.EnrollmentCount)
                    .Take(10)
                    .ToListAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(new ReportsIndexViewModel());
            }
        }

        // GET: Admin/Reports/UserReport
        public async Task<IActionResult> UserReport()
        {
            try
            {
                var userReport = new UserReportViewModel();

                // User Statistics
                userReport.TotalUsers = await _context.Users.CountAsync();
                userReport.VerifiedUsers = await _context.Users.CountAsync(u => u.IsVerified);
                userReport.UnverifiedUsers = await _context.Users.CountAsync(u => !u.IsVerified);

                // User Registration Trend (last 12 months)
                var registrationTrend = new List<MonthlyUserReport>();
                for (int i = 11; i >= 0; i--)
                {
                    var month = DateTime.Now.AddMonths(-i);
                    var startOfMonth = new DateTime(month.Year, month.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    var newUsers = await _context.Users
                        .CountAsync(u => u.CreatedAt >= startOfMonth && u.CreatedAt <= endOfMonth);

                    registrationTrend.Add(new MonthlyUserReport
                    {
                        Month = month.ToString("MM/yyyy"),
                        NewUsers = newUsers
                    });
                }
                userReport.RegistrationTrend = registrationTrend;

                // Recent Users
                userReport.RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(20)
                    .Select(u => new UserSummary
                    {
                        Id = u.Id,
                        FullName = u.FullName ?? "N/A",
                        Email = u.Email ?? "N/A",
                        IsVerified = u.IsVerified,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync();

                return View(userReport);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(new UserReportViewModel());
            }
        }

        // GET: Admin/Reports/CourseReport
        public async Task<IActionResult> CourseReport()
        {
            try
            {
                var courseReport = new CourseReportViewModel();

                // Course Statistics
                courseReport.TotalCourses = await _context.Courses.CountAsync();
                courseReport.PublishedCourses = await _context.Courses.CountAsync(c => c.Status == "Published");
                courseReport.DraftCourses = await _context.Courses.CountAsync(c => c.Status == "Draft");
                courseReport.FreeCourses = await _context.Courses.CountAsync(c => c.Price == 0);
                courseReport.PaidCourses = await _context.Courses.CountAsync(c => c.Price > 0);

                // Average Price
                courseReport.AveragePrice = await _context.Courses
                    .Where(c => c.Price > 0)
                    .AverageAsync(c => (double?)c.Price) ?? 0;

                // Course Creation Trend (last 12 months)
                var creationTrend = new List<MonthlyCourseReport>();
                for (int i = 11; i >= 0; i--)
                {
                    var month = DateTime.Now.AddMonths(-i);
                    var startOfMonth = new DateTime(month.Year, month.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    var newCourses = await _context.Courses
                        .CountAsync(c => c.CreatedAt >= startOfMonth && c.CreatedAt <= endOfMonth);

                    creationTrend.Add(new MonthlyCourseReport
                    {
                        Month = month.ToString("MM/yyyy"),
                        NewCourses = newCourses
                    });
                }
                courseReport.CreationTrend = creationTrend;

                // Courses by Category
                courseReport.CoursesByCategory = await _context.Categories
                    .Include(c => c.Courses)
                    .Select(c => new CategoryReport
                    {
                        CategoryName = c.Name,
                        CourseCount = c.Courses.Count(),
                        PublishedCourses = c.Courses.Count(course => course.Status == "Published"),
                        TotalEnrollments = c.Courses.SelectMany(course => course.Enrollments).Count()
                    })
                    .OrderByDescending(c => c.CourseCount)
                    .ToListAsync();

                return View(courseReport);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(new CourseReportViewModel());
            }
        }

        // GET: Admin/Reports/FinanceReport
        public async Task<IActionResult> FinanceReport()
        {
            try
            {
                var financeReport = new FinanceReportViewModel();

                // Finance Statistics
                financeReport.TotalRevenue = await _context.Finances.SumAsync(f => f.Revenue);
                financeReport.TotalFees = await _context.Finances.SumAsync(f => f.Fee);
                financeReport.NetRevenue = financeReport.TotalRevenue - financeReport.TotalFees;
                financeReport.TotalTransactions = await _context.Finances.CountAsync();

                // Current Year Revenue
                var currentYear = DateTime.Now.Year;
                financeReport.CurrentYearRevenue = await _context.Finances
                    .Where(f => f.Year == currentYear)
                    .SumAsync(f => f.Revenue);

                // Revenue Trend (last 12 months)
                var revenueTrend = new List<MonthlyFinanceReport>();
                for (int i = 11; i >= 0; i--)
                {
                    var month = DateTime.Now.AddMonths(-i);
                    var monthlyFinances = await _context.Finances
                        .Where(f => f.Month == month.Month && f.Year == month.Year)
                        .GroupBy(f => new { f.Month, f.Year })
                        .Select(g => new
                        {
                            Revenue = g.Sum(f => f.Revenue),
                            Fees = g.Sum(f => f.Fee)
                        })
                        .FirstOrDefaultAsync();

                    revenueTrend.Add(new MonthlyFinanceReport
                    {
                        Month = month.ToString("MM/yyyy"),
                        Revenue = monthlyFinances?.Revenue ?? 0,
                        Fees = monthlyFinances?.Fees ?? 0,
                        NetRevenue = (monthlyFinances?.Revenue ?? 0) - (monthlyFinances?.Fees ?? 0)
                    });
                }
                financeReport.RevenueTrend = revenueTrend;

                return View(financeReport);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return View(new FinanceReportViewModel());
            }
        }

        // POST: Admin/Reports/ExportToExcel
        [HttpPost]
        public IActionResult ExportToExcel(string reportType)
        {
            try
            {
                object? data = null;
                string fileName = "";

                switch (reportType.ToLower())
                {
                    case "user":
                        data = GetUserReportData().Result;
                        fileName = "BaoCaoNguoiDung.xlsx";
                        break;
                    case "course":
                        data = GetCourseReportData().Result;
                        fileName = "BaoCaoKhoaHoc.xlsx";
                        break;
                    case "finance":
                        data = GetFinanceReportData().Result;
                        fileName = "BaoCaoTaiChinh.xlsx";
                        break;
                    default:
                        return BadRequest("Loại báo cáo không hợp lệ?");
                }

                if (data == null)
                {
                    return BadRequest("Không thể tạo dữ liệu báo cáo");
                }

                var fileBytes = _reportExportService.ExportToExcel(reportType, data);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest("Có lỗi xảy ra khi xuất Excel: " + ex.Message);
            }
        }

        private async Task<UserReportViewModel> GetUserReportData()
        {
            var userReport = new UserReportViewModel();

            // User Statistics
            userReport.TotalUsers = await _context.Users.CountAsync();
            userReport.VerifiedUsers = await _context.Users.CountAsync(u => u.IsVerified);
            userReport.UnverifiedUsers = await _context.Users.CountAsync(u => !u.IsVerified);

            // User Registration Trend (last 12 months)
            var registrationTrend = new List<MonthlyUserReport>();
            for (int i = 11; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var newUsers = await _context.Users
                    .CountAsync(u => u.CreatedAt >= startOfMonth && u.CreatedAt <= endOfMonth);

                registrationTrend.Add(new MonthlyUserReport
                {
                    Month = month.ToString("MM/yyyy"),
                    NewUsers = newUsers
                });
            }
            userReport.RegistrationTrend = registrationTrend;

            // Recent Users
            userReport.RecentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(20)
                .Select(u => new UserSummary
                {
                    Id = u.Id,
                    FullName = u.FullName ?? "N/A",
                    Email = u.Email ?? "N/A",
                    IsVerified = u.IsVerified,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return userReport;
        }

        private async Task<CourseReportViewModel> GetCourseReportData()
        {
            var courseReport = new CourseReportViewModel();

            // Course Statistics
            courseReport.TotalCourses = await _context.Courses.CountAsync();
            courseReport.PublishedCourses = await _context.Courses.CountAsync(c => c.Status == "Published");
            courseReport.DraftCourses = await _context.Courses.CountAsync(c => c.Status == "Draft");
            courseReport.FreeCourses = await _context.Courses.CountAsync(c => c.Price == 0);
            courseReport.PaidCourses = await _context.Courses.CountAsync(c => c.Price > 0);

            // Average Price
            courseReport.AveragePrice = await _context.Courses
                .Where(c => c.Price > 0)
                .AverageAsync(c => (double?)c.Price) ?? 0;

            // Course Creation Trend (last 12 months)
            var creationTrend = new List<MonthlyCourseReport>();
            for (int i = 11; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var newCourses = await _context.Courses
                    .CountAsync(c => c.CreatedAt >= startOfMonth && c.CreatedAt <= endOfMonth);

                creationTrend.Add(new MonthlyCourseReport
                {
                    Month = month.ToString("MM/yyyy"),
                    NewCourses = newCourses
                });
            }
            courseReport.CreationTrend = creationTrend;

            // Courses by Category
            courseReport.CoursesByCategory = await _context.Categories
                .Include(c => c.Courses)
                .Select(c => new CategoryReport
                {
                    CategoryName = c.Name,
                    CourseCount = c.Courses.Count(),
                    PublishedCourses = c.Courses.Count(course => course.Status == "Published"),
                    TotalEnrollments = c.Courses.SelectMany(course => course.Enrollments).Count()
                })
                .OrderByDescending(c => c.CourseCount)
                .ToListAsync();

            return courseReport;
        }

        private async Task<FinanceReportViewModel> GetFinanceReportData()
        {
            var financeReport = new FinanceReportViewModel();

            // Finance Statistics
            financeReport.TotalRevenue = await _context.Finances.SumAsync(f => f.Revenue);
            financeReport.TotalFees = await _context.Finances.SumAsync(f => f.Fee);
            financeReport.NetRevenue = financeReport.TotalRevenue - financeReport.TotalFees;
            financeReport.TotalTransactions = await _context.Finances.CountAsync();

            // Current Year Revenue
            var currentYear = DateTime.Now.Year;
            financeReport.CurrentYearRevenue = await _context.Finances
                .Where(f => f.Year == currentYear)
                .SumAsync(f => f.Revenue);

            // Revenue Trend (last 12 months)
            var revenueTrend = new List<MonthlyFinanceReport>();
            for (int i = 11; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var monthlyFinances = await _context.Finances
                    .Where(f => f.Month == month.Month && f.Year == month.Year)
                    .GroupBy(f => new { f.Month, f.Year })
                    .Select(g => new
                    {
                        Revenue = g.Sum(f => f.Revenue),
                        Fees = g.Sum(f => f.Fee)
                    })
                    .FirstOrDefaultAsync();

                revenueTrend.Add(new MonthlyFinanceReport
                {
                    Month = month.ToString("MM/yyyy"),
                    Revenue = monthlyFinances?.Revenue ?? 0,
                    Fees = monthlyFinances?.Fees ?? 0,
                    NetRevenue = (monthlyFinances?.Revenue ?? 0) - (monthlyFinances?.Fees ?? 0)
                });
            }
            financeReport.RevenueTrend = revenueTrend;

            return financeReport;
        }
    }
}
