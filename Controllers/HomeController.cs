using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using System.Diagnostics;

namespace ELearningWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Ch�? redirect Admin và Instructor, Student vẫn có th�f xem trang shop
            if (User.Identity!.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                else if (User.IsInRole("Instructor"))
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" }); // Tạm thời cũng vào Admin area
                }
                // Student có th�f xem trang shop bình thường
            }

            var viewModel = new HomeViewModel
            {
                FeaturedCourses = await _context.Courses
                    .Include(c => c.Category)
                    .Where(c => c.Status == "Published")
                    .OrderByDescending(c => c.Enrollments.Count)
                    .Take(6)
                    .ToListAsync(),
                LatestCourses = await _context.Courses
                    .Include(c => c.Category)
                    .Where(c => c.Status == "Published")
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(6)
                    .ToListAsync(),
                Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync(),
                TotalCourses = await _context.Courses.CountAsync(c => c.Status == "Published"),
                TotalStudents = await _context.Enrollments.Select(e => e.UserId).Distinct().CountAsync(),
                TotalInstructors = await _context.Courses.Select(c => c.CreateBy).Distinct().CountAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Courses(int? categoryId, string? search, int page = 1, int pageSize = 12)
        {
            var query = _context.Courses
                .Include(c => c.Category)
                .Where(c => c.Status == "Published");

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Title!.Contains(search) ||
                                       c.Description.Contains(search) ||
                                       c.Category.Name.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            var totalCourses = await query.CountAsync();
            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CoursesViewModel
            {
                Courses = courses,
                Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync(),
                CurrentCategoryId = categoryId,
                SearchTerm = search,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCourses = totalCourses,
                TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> CourseDetail(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Category)
                .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Lessons)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // Ki�fm tra xem user đã đ�fng ký khóa học này chưa
            bool isEnrolled = false;
            if (User.Identity!.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    isEnrolled = await _context.Enrollments
                        .AnyAsync(e => e.CourseId == id && e.UserId == currentUser.Id);
                }
            }

            var viewModel = new CourseDetailViewModel
            {
                Course = course,
                Rating = 4.5m, // Tạm thời hardcode
                ReviewCount = await _context.Enrollments.CountAsync(e => e.CourseId == id),
                IsEnrolled = isEnrolled,
                RelatedCourses = await _context.Courses
                    .Include(c => c.Category)
                    .Where(c => c.CategoryId == course.CategoryId && c.Id != id && c.Status == "Published")
                    .Take(3)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // ViewModels
    public class HomeViewModel
    {
        public IEnumerable<Course> FeaturedCourses { get; set; } = new List<Course>();
        public IEnumerable<Course> LatestCourses { get; set; } = new List<Course>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalInstructors { get; set; }
    }

    public class CoursesViewModel
    {
        public IEnumerable<Course> Courses { get; set; } = new List<Course>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public int? CurrentCategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCourses { get; set; }
        public int TotalPages { get; set; }
    }

    public class CourseDetailViewModel
    {
        public Course Course { get; set; } = null!;
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsEnrolled { get; set; }
        public IEnumerable<Course> RelatedCourses { get; set; } = new List<Course>();
    }


}
