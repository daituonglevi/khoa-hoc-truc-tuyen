using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using ELearningWebsite.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Instructor")]
    public class ChaptersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChaptersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Chapters
        public async Task<IActionResult> Index(int page = 1, string searchTerm = "", int? courseId = null, string status = "")
        {
            var viewModel = new ChapterIndexViewModel
            {
                CurrentPage = page,
                SearchTerm = searchTerm,
                CourseId = courseId,
                Status = status
            };

            // Build query
            var query = _context.Chapters
                .Include(c => c.Course)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => 
                    (c.Name != null && c.Name.Contains(searchTerm)) ||
                    (c.Description != null && c.Description.Contains(searchTerm)) ||
                    (c.Course != null && c.Course.Title != null && c.Course.Title.Contains(searchTerm)));
            }

            // Apply course filter
            if (courseId.HasValue)
            {
                query = query.Where(c => c.CourseId == courseId.Value);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(c => c.Status != null && c.Status == status);
            }

            // Get total count
            viewModel.TotalItems = await query.CountAsync();
            viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalItems / viewModel.PageSize);

            // Get paginated results
            var chapters = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .Select(c => new ChapterListItem
                {
                    Id = c.Id,
                    CourseId = c.CourseId,
                    CourseName = c.Course != null ? (c.Course.Title ?? "Unknown Course") : "Unknown Course",
                    Name = c.Name ?? "Untitled Chapter",
                    Description = c.Description,
                    Status = c.Status ?? "Unknown",
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    CreateBy = c.CreateBy,
                    UpdateBy = c.UpdateBy,
                    CreatedByName = "User " + c.CreateBy,
                    UpdatedByName = c.UpdateBy.HasValue ? "User " + c.UpdateBy.Value : null,
                    LessonsCount = c.Lessons != null ? c.Lessons.Count : 0
                })
                .ToListAsync();

            viewModel.Chapters = chapters;

            // Load filter options
            await LoadFilterOptions(viewModel);

            return View(viewModel);
        }

        // GET: Admin/Chapters/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chương";
                return RedirectToAction(nameof(Index));
            }

            // Lấy danh sách bài học thu�Tc chương
            var lessons = await _context.Lessons
                .Where(l => l.ChapterId == chapter.Id)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new LessonSummary
                {
                    Id = l.Id,
                    Title = l.Title,
                    Type = l.Type,
                    Status = l.Status,
                    CreatedAt = l.CreatedAt,
                    Duration = l.Duration ?? 0,
                })
                .ToListAsync();

            var viewModel = new ChapterDetailsViewModel
            {
                Id = chapter.Id,
                CourseId = chapter.CourseId,
                CourseName = chapter.Course.Title ?? "Unknown Course",
                CourseDescription = chapter.Course.Description ?? "",
                CoursePrice = chapter.Course.Price,
                Name = chapter.Name,
                Description = chapter.Description,
                Status = chapter.Status,
                CreatedAt = chapter.CreatedAt,
                UpdatedAt = chapter.UpdatedAt,
                CreateBy = chapter.CreateBy,
                UpdateBy = chapter.UpdateBy,
                CreatedByName = "User " + chapter.CreateBy,
                UpdatedByName = chapter.UpdateBy.HasValue ? "User " + chapter.UpdateBy.Value : null,
                Lessons = lessons
            };

            return View(viewModel);
        }

        // GET: Admin/Chapters/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ChapterCreateViewModel();
            await LoadCourseOptions(viewModel);
            return View(viewModel);
        }

        // POST: Admin/Chapters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ChapterCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if course exists
                    var course = await _context.Courses.FindAsync(model.CourseId);
                    if (course == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy khóa học";
                        await LoadCourseOptions(model);
                        return View(model);
                    }

                    var chapter = new Chapter
                    {
                        CourseId = model.CourseId,
                        Name = model.Name,
                        Description = model.Description,
                        Status = model.Status,
                        CreatedAt = DateTime.Now,
                        CreateBy = 1 // Simplified - should get from current user
                    };

                    _context.Chapters.Add(chapter);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Tạo chương thành công";
                    return RedirectToAction(nameof(Details), new { id = chapter.Id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                }
            }

            await LoadCourseOptions(model);
            return View(model);
        }

        // GET: Admin/Chapters/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chương";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new ChapterEditViewModel
            {
                Id = chapter.Id,
                CourseId = chapter.CourseId,
                Name = chapter.Name,
                Description = chapter.Description,
                Status = chapter.Status,
                CourseName = chapter.Course.Title ?? "Unknown Course",
                CoursePrice = chapter.Course.Price,
                CreatedAt = chapter.CreatedAt,
                CreatedByName = "User " + chapter.CreateBy
            };

            await LoadCourseOptions(viewModel);
            return View(viewModel);
        }

        // POST: Admin/Chapters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ChapterEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var chapter = await _context.Chapters.FindAsync(id);
                    if (chapter == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy chương";
                        return RedirectToAction(nameof(Index));
                    }

                    // Check if course exists
                    var course = await _context.Courses.FindAsync(model.CourseId);
                    if (course == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy khóa học";
                        await LoadCourseOptions(model);
                        return View(model);
                    }

                    chapter.CourseId = model.CourseId;
                    chapter.Name = model.Name;
                    chapter.Description = model.Description;
                    chapter.Status = model.Status;
                    chapter.UpdatedAt = DateTime.Now;
                    chapter.UpdateBy = 1; // Simplified - should get from current user

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật chương thành công";
                    return RedirectToAction(nameof(Details), new { id = chapter.Id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                }
            }

            // Reload data if validation fails
            var originalChapter = await _context.Chapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (originalChapter != null)
            {
                model.CourseName = originalChapter.Course.Title ?? "Unknown Course";
                model.CoursePrice = originalChapter.Course.Price;
                model.CreatedAt = originalChapter.CreatedAt;
                model.CreatedByName = "User " + originalChapter.CreateBy;
            }

            await LoadCourseOptions(model);
            return View(model);
        }

        // GET: Admin/Chapters/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var chapter = await _context.Chapters
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapter == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chương";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new ChapterDeleteViewModel
            {
                Id = chapter.Id,
                Name = chapter.Name,
                CourseName = chapter.Course.Title ?? "Unknown Course",
                Description = chapter.Description,
                Status = chapter.Status,
                CreatedAt = chapter.CreatedAt,
                LessonsCount = 0, // Simplified since we don't have Lessons table
                Lessons = new List<LessonSummary>()
            };

            return View(viewModel);
        }

        // POST: Admin/Chapters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var chapter = await _context.Chapters.FindAsync(id);
                if (chapter == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy chương";
                    return RedirectToAction(nameof(Index));
                }

                _context.Chapters.Remove(chapter);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa chương thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task LoadFilterOptions(ChapterIndexViewModel viewModel)
        {
            // Load available courses
            var courses = await _context.Courses
                .OrderBy(c => c.Title)
                .Select(c => new ChapterCourseOption
                {
                    Id = c.Id,
                    Title = c.Title ?? "Unknown Course",
                    Price = c.Price,
                    Status = c.Status,
                    ChapterCount = _context.Chapters.Count(ch => ch.CourseId == c.Id)
                })
                .ToListAsync();

            viewModel.AvailableCourses = courses;
        }

        private async Task LoadCourseOptions(ChapterCreateViewModel viewModel)
        {
            var courses = await _context.Courses
                .Where(c => c.Status == "Published" || c.Status == "Draft")
                .OrderBy(c => c.Title)
                .Select(c => new ChapterCourseOption
                {
                    Id = c.Id,
                    Title = c.Title ?? "Unknown Course",
                    Price = c.Price,
                    Status = c.Status,
                    ChapterCount = _context.Chapters.Count(ch => ch.CourseId == c.Id)
                })
                .ToListAsync();

            viewModel.AvailableCourses = courses;
        }

        private async Task LoadCourseOptions(ChapterEditViewModel viewModel)
        {
            var courses = await _context.Courses
                .Where(c => c.Status == "Published" || c.Status == "Draft")
                .OrderBy(c => c.Title)
                .Select(c => new ChapterCourseOption
                {
                    Id = c.Id,
                    Title = c.Title ?? "Unknown Course",
                    Price = c.Price,
                    Status = c.Status,
                    ChapterCount = _context.Chapters.Count(ch => ch.CourseId == c.Id)
                })
                .ToListAsync();

            viewModel.AvailableCourses = courses;
        }

        // GET: Admin/Chapters/Statistics
        public async Task<IActionResult> Statistics()
        {
            var now = DateTime.Now;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var thisYear = new DateTime(now.Year, 1, 1);

            var stats = new ChapterStatisticsViewModel
            {
                TotalChapters = await _context.Chapters.CountAsync(),
                ActiveChapters = await _context.Chapters.CountAsync(c => c.Status == "Active"),
                InactiveChapters = await _context.Chapters.CountAsync(c => c.Status != "Active"),
                ChaptersThisMonth = await _context.Chapters
                    .CountAsync(c => c.CreatedAt >= thisMonth),
                ChaptersThisYear = await _context.Chapters
                    .CountAsync(c => c.CreatedAt >= thisYear),
                TotalLessons = 0 // Simplified since we don't have Lessons table
            };

            // Calculate average lessons per chapter
            stats.AverageLessonsPerChapter = stats.TotalChapters > 0 ?
                (double)stats.TotalLessons / stats.TotalChapters : 0;

            // Get trend data for last 30 days
            var trendData = new List<ChapterTrendData>();
            for (int i = 29; i >= 0; i--)
            {
                var date = DateTime.Now.Date.AddDays(-i);
                var count = await _context.Chapters
                    .CountAsync(c => c.CreatedAt.Date == date);
                trendData.Add(new ChapterTrendData { Date = date, Count = count });
            }
            stats.TrendData = trendData;

            // Get top courses by chapter count
            var topCourses = await _context.Chapters
                .Include(c => c.Course)
                .GroupBy(c => new { c.CourseId, c.Course.Title, c.Course.Price })
                .Select(g => new TopCourseChapterData
                {
                    CourseId = g.Key.CourseId,
                    CourseName = g.Key.Title ?? "Unknown Course",
                    ChapterCount = g.Count(),
                    LessonCount = 0, // Simplified
                    CoursePrice = g.Key.Price
                })
                .OrderByDescending(x => x.ChapterCount)
                .Take(10)
                .ToListAsync();

            stats.TopCourses = topCourses;

            // Get recent chapters
            var recentChapters = await _context.Chapters
                .Include(c => c.Course)
                .OrderByDescending(c => c.CreatedAt)
                .Take(10)
                .Select(c => new RecentChapterData
                {
                    Id = c.Id,
                    Name = c.Name,
                    CourseName = c.Course.Title ?? "Unknown Course",
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    CreatedByName = "User " + c.CreateBy
                })
                .ToListAsync();

            stats.RecentChapters = recentChapters;

            // Get status distribution
            var statusDistribution = await _context.Chapters
                .GroupBy(c => c.Status)
                .Select(g => new ChapterStatusData
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = stats.TotalChapters > 0 ? (double)g.Count() / stats.TotalChapters * 100 : 0
                })
                .ToListAsync();

            stats.StatusDistribution = statusDistribution;

            return View(stats);
        }

        // POST: Admin/Chapters/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var chapter = await _context.Chapters.FindAsync(id);
                if (chapter == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy chương" });
                }

                chapter.Status = chapter.Status == "Active" ? "Inactive" : "Active";
                chapter.UpdatedAt = DateTime.Now;
                chapter.UpdateBy = 1; // Simplified - should get from current user

                await _context.SaveChangesAsync();

                return Json(new {
                    success = true,
                    message = $"Đã {(chapter.Status == "Active" ? "kích hoạt" : "vô hi�?u hóa")} chương",
                    newStatus = chapter.Status
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có l�-i xảy ra: " + ex.Message });
            }
        }
    }
}
