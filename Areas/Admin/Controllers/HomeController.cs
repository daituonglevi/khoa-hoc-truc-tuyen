using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                TotalRevenue = await _context.Enrollments.SumAsync(e => (decimal?)e.Course.Price) ?? 0,

                RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                RecentCourses = await _context.Courses
                    .Include(c => c.Category)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(5)
                    .ToListAsync(),

                RecentEnrollments = new List<Enrollment>(), // Tạm thời đ�f trđng

                // Thđng kê theo tháng
                MonthlyStats = await GetMonthlyStats()
            };

            return View(viewModel);
        }

        private async Task<List<MonthlyStatistic>> GetMonthlyStats()
        {
            var stats = new List<MonthlyStatistic>();
            var currentDate = DateTime.Now;

            for (int i = 5; i >= 0; i--)
            {
                var month = currentDate.AddMonths(-i);
                var startOfMonth = new DateTime(month.Year, month.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var enrollmentCount = await _context.Enrollments
                    .CountAsync(e => e.EnrollmentDate >= startOfMonth && e.EnrollmentDate <= endOfMonth);

                var revenue = await _context.Enrollments
                    .Where(e => e.EnrollmentDate >= startOfMonth && e.EnrollmentDate <= endOfMonth)
                    .SumAsync(e => (decimal?)e.Course.Price) ?? 0;

                stats.Add(new MonthlyStatistic
                {
                    Month = month.ToString("MM/yyyy"),
                    Enrollments = enrollmentCount,
                    Revenue = revenue
                });
            }

            return stats;
        }
    }

    // ViewModels
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public decimal TotalRevenue { get; set; }

        public IEnumerable<ApplicationUser> RecentUsers { get; set; } = new List<ApplicationUser>();
        public IEnumerable<Course> RecentCourses { get; set; } = new List<Course>();
        public IEnumerable<Enrollment> RecentEnrollments { get; set; } = new List<Enrollment>();

        public List<MonthlyStatistic> MonthlyStats { get; set; } = new List<MonthlyStatistic>();
    }

    public class MonthlyStatistic
    {
        public string Month { get; set; } = string.Empty;
        public int Enrollments { get; set; }
        public decimal Revenue { get; set; }
    }
}
