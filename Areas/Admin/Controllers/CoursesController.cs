using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using System.ComponentModel.DataAnnotations;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ApplicationDbContext context, ILogger<CoursesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "", int? categoryId = null, string status = "")
        {
            ViewData["Title"] = "Quản lý Courses";

            var query = _context.Courses.Include(c => c.Category).AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Title.Contains(search) || c.Description.Contains(search));
                ViewData["Search"] = search;
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
                ViewData["CategoryId"] = categoryId;
            }

            // Status filter
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status == status);
                ViewData["Status"] = status;
            }

            var totalCourses = await query.CountAsync();
            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get categories for filter dropdown
            var categories = await _context.Categories
                .Where(c => c.Status == 1) // 1 = Active
                .ToListAsync();

            var viewModel = new CoursesIndexViewModel
            {
                Courses = courses,
                Categories = categories,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCourses = totalCourses,
                TotalPages = (int)Math.Ceiling((double)totalCourses / pageSize),
                Search = search,
                SelectedCategoryId = categoryId,
                SelectedStatus = status
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi tiết Course";

            var course = await _context.Courses
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // Get recent enrollments for this course (without CompletedDate)
            var enrollments = await _context.Enrollments
                .Where(e => e.CourseId == id)
                .OrderByDescending(e => e.EnrollmentDate)
                .Take(10)
                .Select(e => new Enrollment
                {
                    Id = e.Id,
                    CourseId = e.CourseId,
                    UserId = e.UserId,
                    EnrollmentDate = e.EnrollmentDate,
                    ExpiredDate = e.ExpiredDate,
                    Status = e.Status,
                    Progress = e.Progress
                })
                .ToListAsync();

            var viewModel = new CourseDetailsViewModel
            {
                Course = course,
                RecentEnrollments = enrollments,
                TotalEnrollments = await _context.Enrollments.CountAsync(e => e.CourseId == id)
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Thêm Course m�>i";
            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View(new Course());
        }

        public async Task<IActionResult> CreateNew()
        {
            ViewData["Title"] = "Thêm Course m�>i";
            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View(new Course());
        }

        public async Task<IActionResult> CreateTest()
        {
            ViewData["Title"] = "Test Thêm Course";
            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View();
        }

        public async Task<IActionResult> EditTest(int id)
        {
            ViewData["Title"] = "Test Ch�?nh sửa Course";

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View(course);
        }

        public IActionResult TestUpdate()
        {
            ViewData["Title"] = "Test Update";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWithImage(string Title, int CategoryId, double Price, string Description = "", string ThumbnailUrl = "", string PreviewVideo = "", IFormFile ThumbnailFile = null)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TempData["ErrorMessage"] = "Tên khóa học là bắt bu�Tc";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View("CreateTest");
                }

                if (CategoryId <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn danh mục";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View("CreateTest");
                }

                string thumbnailPath = "/images/default-course.jpg";

                // Xử lý upload file hình ảnh
                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    try
                    {
                        // Ki�fm tra file type
                        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                        if (!allowedTypes.Contains(ThumbnailFile.ContentType.ToLower()))
                        {
                            TempData["ErrorMessage"] = "Ch�? chấp nhận file hình ảnh (JPG, PNG, GIF)";
                            ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                            return View("CreateTest");
                        }

                        // Ki�fm tra kích thư�>c file (5MB)
                        if (ThumbnailFile.Length > 5 * 1024 * 1024)
                        {
                            TempData["ErrorMessage"] = "File hình ảnh không được vượt quá 5MB";
                            ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                            return View("CreateTest");
                        }

                        // Tạo tên file unique
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ThumbnailFile.FileName);
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "courses");

                        // Tạo thư mục nếu chưa tôn tại
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // Lưu file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ThumbnailFile.CopyToAsync(fileStream);
                        }

                        thumbnailPath = $"/images/courses/{fileName}";

                        // Debug: Log thông tin upload
                        Console.WriteLine($"File uploaded successfully: {fileName}");
                        Console.WriteLine($"File path: {filePath}");
                        Console.WriteLine($"Thumbnail path: {thumbnailPath}");
                    }
                    catch (Exception uploadEx)
                    {
                        TempData["ErrorMessage"] = "L�-i upload file: " + uploadEx.Message;
                        ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                        return View("CreateTest");
                    }
                }
                // Nếu không upload file nhưng có URL
                else if (!string.IsNullOrWhiteSpace(ThumbnailUrl))
                {
                    thumbnailPath = ThumbnailUrl;
                }

                // Debug: Log thông tin trư�>c khi lưu
                Console.WriteLine($"Saving course with thumbnail: {thumbnailPath}");

                // Sử dụng ADO.NET đ�f insert
                var connectionString = _context.Database.GetConnectionString();
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "INSERT INTO Courses (Title, CategoryId, Price, Description, Thumbnail, PreviewVideo, Status, CreatedAt, CreateBy) VALUES (@Title, @CategoryId, @Price, @Description, @Thumbnail, @PreviewVideo, @Status, @CreatedAt, @CreateBy)",
                        connection);

                    command.Parameters.AddWithValue("@Title", Title.Trim());
                    command.Parameters.AddWithValue("@CategoryId", CategoryId);
                    command.Parameters.AddWithValue("@Price", Price);
                    command.Parameters.AddWithValue("@Description", Description.Trim());
                    command.Parameters.AddWithValue("@Thumbnail", thumbnailPath);
                    command.Parameters.AddWithValue("@PreviewVideo", PreviewVideo.Trim());
                    command.Parameters.AddWithValue("@Status", "Draft");
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@CreateBy", 1);

                    var result = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"Database insert result: {result} rows affected");
                }

                TempData["SuccessMessage"] = "Thêm khóa học thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;

                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += " Chi tiết: " + ex.InnerException.Message;
                }
            }

            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View("CreateTest");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWithImage(int id, Course course, IFormFile ThumbnailFile = null)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            try
            {
                Console.WriteLine($"=== EDIT WITH IMAGE DEBUG START ===");
                Console.WriteLine($"Course ID: {course.Id}");
                Console.WriteLine($"Current Thumbnail: {course.Thumbnail}");
                Console.WriteLine($"ThumbnailFile is null: {ThumbnailFile == null}");
                if (ThumbnailFile != null)
                {
                    Console.WriteLine($"ThumbnailFile Length: {ThumbnailFile.Length}");
                    Console.WriteLine($"ThumbnailFile Name: {ThumbnailFile.FileName}");
                    Console.WriteLine($"ThumbnailFile ContentType: {ThumbnailFile.ContentType}");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(course.Title))
                {
                    TempData["ErrorMessage"] = "Tên khóa học là bắt bu�Tc";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View("EditTest", course);
                }

                if (course.CategoryId <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn danh mục";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View("EditTest", course);
                }

                // Xử lý upload file hình ảnh nếu có
                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    try
                    {
                        // Ki�fm tra file type
                        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                        if (!allowedTypes.Contains(ThumbnailFile.ContentType.ToLower()))
                        {
                            TempData["ErrorMessage"] = "Ch�? chấp nhận file hình ảnh (JPG, PNG, GIF)";
                            ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                            return View("EditTest", course);
                        }

                        // Ki�fm tra kích thư�>c file (5MB)
                        if (ThumbnailFile.Length > 5 * 1024 * 1024)
                        {
                            TempData["ErrorMessage"] = "File hình ảnh không được vượt quá 5MB";
                            ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                            return View("EditTest", course);
                        }

                        // Tạo tên file unique
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ThumbnailFile.FileName);
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "courses");

                        // Tạo thư mục nếu chưa tôn tại
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // Lưu file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ThumbnailFile.CopyToAsync(fileStream);
                        }

                        // Cập nhật đường dẫn thumbnail
                        course.Thumbnail = $"/images/courses/{fileName}";

                        Console.WriteLine($"File uploaded successfully: {fileName}");
                        Console.WriteLine($"New thumbnail path: {course.Thumbnail}");
                    }
                    catch (Exception uploadEx)
                    {
                        TempData["ErrorMessage"] = "L�-i upload file: " + uploadEx.Message;
                        ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                        return View("EditTest", course);
                    }
                }

                // Sử dụng ADO.NET đ�f update
                var connectionString = _context.Database.GetConnectionString();
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"UPDATE Courses SET
                            Title = @Title,
                            CategoryId = @CategoryId,
                            Price = @Price,
                            Description = @Description,
                            Thumbnail = @Thumbnail,
                            PreviewVideo = @PreviewVideo,
                            Status = @Status,
                            UpdatedAt = @UpdatedAt,
                            UpdateBy = @UpdateBy
                          WHERE Id = @Id",
                        connection);

                    command.Parameters.AddWithValue("@Id", course.Id);
                    command.Parameters.AddWithValue("@Title", course.Title.Trim());
                    command.Parameters.AddWithValue("@CategoryId", course.CategoryId);
                    command.Parameters.AddWithValue("@Price", course.Price);
                    command.Parameters.AddWithValue("@Description", course.Description.Trim());
                    command.Parameters.AddWithValue("@Thumbnail", course.Thumbnail);
                    command.Parameters.AddWithValue("@PreviewVideo", course.PreviewVideo.Trim());
                    command.Parameters.AddWithValue("@Status", course.Status);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateBy", 1);

                    var result = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"Database update result: {result} rows affected");
                }

                TempData["SuccessMessage"] = "Cập nhật khóa học thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;

                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += " Chi tiết: " + ex.InnerException.Message;
                }
            }

            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View("EditTest", course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCourseSimple(int courseId, string title, string thumbnail)
        {
            try
            {
                Console.WriteLine($"=== UPDATE COURSE SIMPLE DEBUG START ===");
                Console.WriteLine($"Course ID: {courseId}");
                Console.WriteLine($"Title: {title}");
                Console.WriteLine($"Thumbnail: {thumbnail}");

                // Sử dụng ADO.NET đ�f update ch�? thumbnail
                var connectionString = _context.Database.GetConnectionString();
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "UPDATE Courses SET Title = @Title, Thumbnail = @Thumbnail, UpdatedAt = @UpdatedAt WHERE Id = @Id",
                        connection);

                    command.Parameters.AddWithValue("@Id", courseId);
                    command.Parameters.AddWithValue("@Title", title ?? "");
                    command.Parameters.AddWithValue("@Thumbnail", thumbnail ?? "/images/default-course.jpg");
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                    var result = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"Database update result: {result} rows affected");
                }

                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new { success = false, message = "Có l�-i xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSimple(string Title, int CategoryId, double Price)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TempData["ErrorMessage"] = "Tên khóa học là bắt bu�Tc";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View("Create", new Course());
                }

                if (CategoryId <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn danh mục";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View("Create", new Course());
                }

                // Sử dụng ADO.NET thuần túy
                var connectionString = _context.Database.GetConnectionString();
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = new Microsoft.Data.SqlClient.SqlCommand(
                        "INSERT INTO Courses (Title, CategoryId, Price, Description, Thumbnail, PreviewVideo, Status, CreatedAt, CreateBy) VALUES (@Title, @CategoryId, @Price, @Description, @Thumbnail, @PreviewVideo, @Status, @CreatedAt, @CreateBy)",
                        connection);

                    command.Parameters.AddWithValue("@Title", Title.Trim());
                    command.Parameters.AddWithValue("@CategoryId", CategoryId);
                    command.Parameters.AddWithValue("@Price", Price);
                    command.Parameters.AddWithValue("@Description", "");
                    command.Parameters.AddWithValue("@Thumbnail", "/images/default-course.jpg");
                    command.Parameters.AddWithValue("@PreviewVideo", "");
                    command.Parameters.AddWithValue("@Status", "Draft");
                    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@CreateBy", 1);

                    await command.ExecuteNonQueryAsync();
                }

                TempData["SuccessMessage"] = "Thêm khóa học thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;

                // Log chi tiết l�-i đ�f debug
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += " Chi tiết: " + ex.InnerException.Message;
                }
            }

            // Reload categories if validation fails
            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View("Create", new Course());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOld(string Title, int CategoryId, double Price)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(Title))
                {
                    TempData["ErrorMessage"] = "Tên khóa học là bắt bu�Tc";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View("Create", new Course());
                }

                if (CategoryId <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn danh mục";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View("Create", new Course());
                }

                // Sử dụng SQL thô đơn giản nhất
                var sql = @"INSERT INTO Courses (Title, CategoryId, Price, Description, Thumbnail, PreviewVideo, Status, CreatedAt, CreateBy)
                           VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8)";

                await _context.Database.ExecuteSqlRawAsync(sql,
                    Title.Trim(),
                    CategoryId,
                    Price,
                    "",
                    "/images/default-course.jpg",
                    "",
                    "Draft",
                    DateTime.Now,
                    1);

                TempData["SuccessMessage"] = "Thêm khóa học thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;

                // Log chi tiết l�-i đ�f debug
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += " Chi tiết: " + ex.InnerException.Message;
                }
            }

            // Reload categories if validation fails
            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View("Create", new Course());
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Ch�?nh sửa Course";

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View(course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course, IFormFile ThumbnailFile = null)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            try
            {
                Console.WriteLine($"=== EDIT DEBUG START ===");
                Console.WriteLine($"Course ID: {course.Id}");
                Console.WriteLine($"Current Thumbnail: {course.Thumbnail}");
                Console.WriteLine($"ThumbnailFile is null: {ThumbnailFile == null}");
                if (ThumbnailFile != null)
                {
                    Console.WriteLine($"ThumbnailFile Length: {ThumbnailFile.Length}");
                    Console.WriteLine($"ThumbnailFile Name: {ThumbnailFile.FileName}");
                    Console.WriteLine($"ThumbnailFile ContentType: {ThumbnailFile.ContentType}");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(course.Title))
                {
                    TempData["ErrorMessage"] = "Tên khóa học là bắt bu�Tc";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View(course);
                }

                if (course.CategoryId <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn danh mục";
                    ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                    return View(course);
                }

                // Xử lý upload file hình ảnh nếu có
                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    try
                    {
                        // Ki�fm tra file type
                        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                        if (!allowedTypes.Contains(ThumbnailFile.ContentType.ToLower()))
                        {
                            TempData["ErrorMessage"] = "Ch�? chấp nhận file hình ảnh (JPG, PNG, GIF)";
                            ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                            return View(course);
                        }

                        // Ki�fm tra kích thư�>c file (5MB)
                        if (ThumbnailFile.Length > 5 * 1024 * 1024)
                        {
                            TempData["ErrorMessage"] = "File hình ảnh không được vượt quá 5MB";
                            ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                            return View(course);
                        }

                        // Tạo tên file unique
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ThumbnailFile.FileName);
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "courses");

                        // Tạo thư mục nếu chưa tôn tại
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // Lưu file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ThumbnailFile.CopyToAsync(fileStream);
                        }

                        // Cập nhật đường dẫn thumbnail
                        course.Thumbnail = $"/images/courses/{fileName}";

                        Console.WriteLine($"File uploaded successfully: {fileName}");
                        Console.WriteLine($"New thumbnail path: {course.Thumbnail}");
                    }
                    catch (Exception uploadEx)
                    {
                        TempData["ErrorMessage"] = "L�-i upload file: " + uploadEx.Message;
                        ViewBag.Categories = await _context.Categories.Where(c => c.Status == 1).ToListAsync();
                        return View(course);
                    }
                }

                // Sử dụng ADO.NET đ�f update
                var connectionString = _context.Database.GetConnectionString();
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = new Microsoft.Data.SqlClient.SqlCommand(
                        @"UPDATE Courses SET
                            Title = @Title,
                            CategoryId = @CategoryId,
                            Price = @Price,
                            Description = @Description,
                            Thumbnail = @Thumbnail,
                            PreviewVideo = @PreviewVideo,
                            Status = @Status,
                            UpdatedAt = @UpdatedAt,
                            UpdateBy = @UpdateBy,
                            LimitDay = @LimitDay
                          WHERE Id = @Id",
                        connection);

                    command.Parameters.AddWithValue("@Id", course.Id);
                    command.Parameters.AddWithValue("@Title", course.Title.Trim());
                    command.Parameters.AddWithValue("@CategoryId", course.CategoryId);
                    command.Parameters.AddWithValue("@Price", course.Price);
                    command.Parameters.AddWithValue("@Description", course.Description.Trim());
                    command.Parameters.AddWithValue("@Thumbnail", course.Thumbnail);
                    command.Parameters.AddWithValue("@PreviewVideo", course.PreviewVideo.Trim());
                    command.Parameters.AddWithValue("@Status", course.Status);
                    command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@UpdateBy", 1);
                    command.Parameters.AddWithValue("@LimitDay", course.LimitDay.HasValue ? (object)course.LimitDay.Value : DBNull.Value);

                    var result = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"Database update result: {result} rows affected");
                }

                TempData["SuccessMessage"] = "Cập nhật khóa học thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;

                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += " Chi tiết: " + ex.InnerException.Message;
                }
            }

            // Reload categories if validation fails
            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == 1)
                .ToListAsync();

            return View(course);
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus([FromForm] int id, [FromForm] string status)
        {
            try
            {
                // Ki�fm tra course có tôn tại không
                var connectionString = _context.Database.GetConnectionString();
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Ki�fm tra course tôn tại
                    var checkCommand = new Microsoft.Data.SqlClient.SqlCommand(
                        "SELECT COUNT(*) FROM Courses WHERE Id = @Id", connection);
                    checkCommand.Parameters.AddWithValue("@Id", id);

                    var exists = (int)await checkCommand.ExecuteScalarAsync() > 0;
                    if (!exists)
                    {
                        return Json(new { success = false, message = "Course không tôn tại" });
                    }

                    // Cập nhật trạng thái
                    var updateCommand = new Microsoft.Data.SqlClient.SqlCommand(
                        "UPDATE Courses SET Status = @Status, UpdatedAt = @UpdatedAt WHERE Id = @Id",
                        connection);

                    updateCommand.Parameters.AddWithValue("@Id", id);
                    updateCommand.Parameters.AddWithValue("@Status", status);
                    updateCommand.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                    await updateCommand.ExecuteNonQueryAsync();
                }

                return Json(new {
                    success = true,
                    message = $"Đã cập nhật trạng thái course thành {status}",
                    status = status
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có l�-i xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromForm] int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return Json(new { success = false, message = "Course không tôn tại" });
            }

            // Check if course has enrollments
            var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.CourseId == id);
            if (hasEnrollments)
            {
                return Json(new { success = false, message = "Không th�f xóa course đã có học viên đ�fng ký" });
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa course thành công" });
        }
    }

    // ViewModels
    public class CoursesIndexViewModel
    {
        public IEnumerable<Course> Courses { get; set; } = new List<Course>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalCourses { get; set; }
        public int TotalPages { get; set; }
        public string Search { get; set; } = string.Empty;
        public int? SelectedCategoryId { get; set; }
        public string SelectedStatus { get; set; } = string.Empty;
    }

    public class CourseDetailsViewModel
    {
        public Course Course { get; set; } = new Course();
        public IEnumerable<Enrollment> RecentEnrollments { get; set; } = new List<Enrollment>();
        public int TotalEnrollments { get; set; }
    }


}
