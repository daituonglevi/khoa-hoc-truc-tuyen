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
    [Authorize(Roles = "Admin,Instructor")]
    public class LessonProgressesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LessonProgressesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/LessonProgresses
        public async Task<IActionResult> Index(int page = 1, string searchTerm = "", int? lessonId = null,
            int? userId = null, string status = "", float? minProgress = null, float? maxProgress = null)
        {
            var viewModel = new LessonProgressIndexViewModel
            {
                CurrentPage = page,
                SearchTerm = searchTerm,
                LessonId = lessonId,
                UserId = userId,
                Status = status,
                MinProgress = minProgress,
                MaxProgress = maxProgress
            };

            try
            {
                // Build base query
                var query = _context.LessonProgresses
                    .Include(lp => lp.User)
                    .Include(lp => lp.Lesson)
                        .ThenInclude(l => l.Chapter)
                            .ThenInclude(c => c.Course)
                    .AsNoTracking();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(lp => 
                        (lp.User != null && (
                            lp.User.UserName.Contains(searchTerm) ||
                            lp.User.Email.Contains(searchTerm)
                        )) ||
                        (lp.Status != null && lp.Status.Contains(searchTerm))
                    );
                }

                // Apply lesson filter
                if (lessonId.HasValue)
                {
                    query = query.Where(lp => lp.LessonId == lessonId.Value);
                }

                // Apply user filter
                if (userId.HasValue)
                {
                    query = query.Where(lp => lp.UserId == userId.Value);
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    switch (status.ToLower())
                    {
                        case "completed":
                            query = query.Where(lp => lp.ProgressPercentage >= 100);
                            break;
                        case "in-progress":
                            query = query.Where(lp => lp.ProgressPercentage > 0 && lp.ProgressPercentage < 100);
                            break;
                        case "not-started":
                            query = query.Where(lp => lp.ProgressPercentage == 0);
                            break;
                    }
                }

                // Apply progress range filter
                if (minProgress.HasValue)
                {
                    query = query.Where(lp => lp.ProgressPercentage >= minProgress.Value);
                }
                if (maxProgress.HasValue)
                {
                    query = query.Where(lp => lp.ProgressPercentage <= maxProgress.Value);
                }

                // Calculate summary statistics
                var allProgresses = await query.ToListAsync();
                viewModel.TotalItems = allProgresses.Count;
                viewModel.CompletedCount = allProgresses.Count(p => p.ProgressPercentage >= 100);
                viewModel.InProgressCount = allProgresses.Count(p => p.ProgressPercentage > 0 && p.ProgressPercentage < 100);
                viewModel.NotStartedCount = allProgresses.Count(p => p.ProgressPercentage == 0);
                viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalItems / viewModel.PageSize);

                // Get paginated results
                var pagedProgresses = allProgresses
                    .OrderByDescending(lp => lp.UpdatedAt ?? lp.CreatedAt)
                    .Skip((page - 1) * viewModel.PageSize)
                    .Take(viewModel.PageSize)
                    .ToList();

                // Map to view model
                var progresses = pagedProgresses.Select(lp => new LessonProgressListItem
                {
                    Id = lp.Id,
                    LessonId = lp.LessonId,
                    LessonTitle = lp.Lesson?.Title ?? $"Bài học {lp.LessonId}",
                    ChapterName = lp.Lesson?.Chapter?.Name ?? "Unknown Chapter",
                    CourseName = lp.Lesson?.Chapter?.Course?.Title ?? "Unknown Course",
                    UserId = lp.UserId,
                    UserName = lp.User?.UserName ?? "Unknown",
                    UserEmail = lp.User?.Email ?? "Unknown",
                    ProgressPercentage = lp.ProgressPercentage,
                    TimeSpent = lp.TimeSpent,
                    CreatedAt = lp.CreatedAt,
                    UpdatedAt = lp.UpdatedAt,
                    Status = lp.Status ?? (lp.ProgressPercentage >= 100 ? "Completed" : 
                             lp.ProgressPercentage > 0 ? "In Progress" : "Not Started"),
                    Passing = lp.Passing,
                    CountDoing = lp.CountDoing,
                    HighestMark = lp.HighestMark
                }).ToList();

                viewModel.LessonProgresses = progresses;

                // Load filter options
                await LoadFilterOptions(viewModel);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in LessonProgresses/Index: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Có l�-i xảy ra khi tải dữ li�?u: " + ex.Message;
                return View(viewModel);
            }
        }

        // GET: Admin/LessonProgresses/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var progress = await _context.LessonProgresses
                .Include(lp => lp.User)
                .FirstOrDefaultAsync(lp => lp.Id == id);

            if (progress == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tiến đ�T học tập";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new LessonProgressDetailsViewModel
            {
                Id = progress.Id,
                LessonId = progress.LessonId,
                UserId = progress.UserId,
                UserName = progress.User?.UserName ?? "Unknown",
                UserEmail = progress.User?.Email ?? "Unknown",
                ProgressPercentage = progress.ProgressPercentage,
                TimeSpent = progress.TimeSpent,
                CreatedAt = progress.CreatedAt,
                UpdatedAt = progress.UpdatedAt,
                Status = progress.Status,
                Passing = progress.Passing,
                CountDoing = progress.CountDoing,
                HighestMark = progress.HighestMark,
                ProgressHistory = new List<LessonProgressHistoryItem>
                {
                    new LessonProgressHistoryItem
                    {
                        Date = progress.CreatedAt,
                        ProgressPercentage = 0,
                        Action = "Started",
                        Notes = "Bắt đầu học bài"
                    }
                }
            };

            if (progress.UpdatedAt.HasValue && progress.ProgressPercentage > 0)
            {
                viewModel.ProgressHistory.Add(new LessonProgressHistoryItem
                {
                    Date = progress.UpdatedAt.Value,
                    ProgressPercentage = progress.ProgressPercentage,
                    TimeSpent = progress.TimeSpent,
                    Action = progress.ProgressPercentage >= 100 ? "Completed" : "Updated",
                    Notes = progress.ProgressPercentage >= 100 ? "Hoàn thành bài học" : "Cập nhật tiến đ�T"
                });
            }

            return View(viewModel);
        }

        // GET: Admin/LessonProgresses/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new LessonProgressCreateViewModel();
            await LoadCreateEditOptions(viewModel);
            return View(viewModel);
        }

        // POST: Admin/LessonProgresses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LessonProgressCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var progress = new LessonProgress
                    {
                        LessonId = model.LessonId,
                        UserId = model.UserId,
                        ProgressPercentage = model.ProgressPercentage,
                        TimeSpent = model.TimeSpent,
                        Status = model.Status,
                        Passing = model.Passing,
                        CountDoing = model.CountDoing,
                        HighestMark = model.HighestMark,
                        CreatedAt = DateTime.Now
                    };

                _context.Add(progress);
                    await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm tiến đ�T học tập thành công";
                return RedirectToAction(nameof(Index));
            }

            await LoadCreateEditOptions(model);
            return View(model);
        }

        // GET: Admin/LessonProgresses/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var progress = await _context.LessonProgresses
                .Include(lp => lp.User)
                .FirstOrDefaultAsync(lp => lp.Id == id);

            if (progress == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tiến đ�T học tập";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new LessonProgressEditViewModel
            {
                Id = progress.Id,
                LessonId = progress.LessonId,
                UserId = progress.UserId,
                ProgressPercentage = progress.ProgressPercentage,
                TimeSpent = progress.TimeSpent,
                Status = progress.Status,
                Passing = progress.Passing,
                CountDoing = progress.CountDoing,
                HighestMark = progress.HighestMark,
                CreatedAt = progress.CreatedAt
            };

            await LoadCreateEditOptions(viewModel);
            return View(viewModel);
        }

        // POST: Admin/LessonProgresses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LessonProgressEditViewModel model)
        {
            if (id != model.Id)
            {
                TempData["ErrorMessage"] = "ID không hợp l�?";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var progress = await _context.LessonProgresses.FindAsync(id);
                    if (progress == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy tiến đ�T học tập";
                        return RedirectToAction(nameof(Index));
                    }

                    progress.LessonId = model.LessonId;
                    progress.UserId = model.UserId;
                    progress.ProgressPercentage = model.ProgressPercentage;
                    progress.TimeSpent = model.TimeSpent;
                    progress.Status = model.Status;
                    progress.Passing = model.Passing;
                    progress.CountDoing = model.CountDoing;
                    progress.HighestMark = model.HighestMark;
                    progress.UpdatedAt = DateTime.Now;

                    _context.Update(progress);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật tiến đ�T học tập thành công";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await LessonProgressExists(model.Id))
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy tiến đ�T học tập";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await LoadCreateEditOptions(model);
            return View(model);
        }

        // GET: Admin/LessonProgresses/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var progress = await _context.LessonProgresses
                .Include(lp => lp.User)
                .FirstOrDefaultAsync(lp => lp.Id == id);

            if (progress == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tiến đ�T học tập";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new LessonProgressDeleteViewModel
            {
                Id = progress.Id,
                LessonId = progress.LessonId,
                UserId = progress.UserId,
                UserName = progress.User?.UserName ?? "Unknown",
                UserEmail = progress.User?.Email ?? "Unknown",
                ProgressPercentage = progress.ProgressPercentage,
                TimeSpent = progress.TimeSpent,
                CreatedAt = progress.CreatedAt,
                Status = progress.Status,
                CountDoing = progress.CountDoing,
                HighestMark = progress.HighestMark
            };

            return View(viewModel);
        }

        // POST: Admin/LessonProgresses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var progress = await _context.LessonProgresses.FindAsync(id);
            if (progress != null)
            {
                _context.LessonProgresses.Remove(progress);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa tiến đ�T học tập thành công";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy tiến đ�T học tập";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadFilterOptions(LessonProgressIndexViewModel viewModel)
        {
            try
            {
                // First check if we have any lessons
                var lessonCount = await _context.Lessons.CountAsync();
                Console.WriteLine($"Total lessons in database: {lessonCount}");

                // Load available lessons with titles
                var lessons = await _context.Lessons.ToListAsync();
                var availableLessons = lessons.Select(l => new LessonOption 
                { 
                    Id = l.Id, 
                    Title = l.Title ?? $"Bài học {l.Id}"
                }).ToList();

                Console.WriteLine($"Mapped {availableLessons.Count} lessons");
                viewModel.AvailableLessons = availableLessons;

                // Check if we have any users
                var userCount = await _userManager.Users.CountAsync();
                Console.WriteLine($"Total users in database: {userCount}");

                // Load available users
                var dbUsers = await _userManager.Users.ToListAsync();
                var users = dbUsers.Select(u => new UserOption
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "Unknown",
                    Email = u.Email ?? "Unknown"
                }).ToList();

                Console.WriteLine($"Mapped {users.Count} users");
                viewModel.AvailableUsers = users;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in LoadFilterOptions: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                viewModel.AvailableLessons = new List<LessonOption>();
                viewModel.AvailableUsers = new List<UserOption>();
            }
        }

        private async Task LoadCreateEditOptions(LessonProgressCreateViewModel viewModel)
        {
            // Load available lessons
            var lessons = await _context.Lessons
                .Select(l => new LessonOption
                {
                    Id = l.Id,
                    Title = l.Title ?? $"Bài học {l.Id}"
                })
                .OrderBy(l => l.Id)
                .ToListAsync();

            viewModel.AvailableLessons = lessons;

            // Load available users
            var users = await _userManager.Users
                .Select(u => new UserOption
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "Unknown",
                    Email = u.Email ?? "Unknown"
                })
                .ToListAsync();

            viewModel.AvailableUsers = users;
        }

        private async Task LoadCreateEditOptions(LessonProgressEditViewModel viewModel)
        {
            // Load available lessons
            var lessons = await _context.Lessons
                .Select(l => new LessonOption
                {
                    Id = l.Id,
                    Title = l.Title ?? $"Bài học {l.Id}"
                })
                .OrderBy(l => l.Id)
                .ToListAsync();

            viewModel.AvailableLessons = lessons;

            // Load available users
            var users = await _userManager.Users
                .Select(u => new UserOption
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "Unknown",
                    Email = u.Email ?? "Unknown"
                })
                .ToListAsync();

            viewModel.AvailableUsers = users;
        }

        private async Task<bool> LessonProgressExists(int id)
        {
            return await _context.LessonProgresses.AnyAsync(e => e.Id == id);
        }

        // GET: Admin/LessonProgresses/Statistics
        public async Task<IActionResult> Statistics()
        {
            var viewModel = new LessonProgressStatisticsViewModel();

            // Get basic statistics
            viewModel.TotalProgresses = await _context.LessonProgresses.CountAsync();
            viewModel.CompletedProgresses = await _context.LessonProgresses.CountAsync(lp => lp.ProgressPercentage >= 100);
            viewModel.NotStartedProgresses = await _context.LessonProgresses.CountAsync(lp => lp.ProgressPercentage == 0);
            viewModel.InProgressProgresses = await _context.LessonProgresses.CountAsync(lp => lp.ProgressPercentage > 0 && lp.ProgressPercentage < 100);

            // Calculate averages
            var averages = await _context.LessonProgresses
                .GroupBy(x => 1)
                .Select(g => new
                {
                    AverageProgress = g.Average(lp => lp.ProgressPercentage),
                    AverageTimeSpent = g.Average(lp => lp.TimeSpent ?? 0)
                })
                .FirstOrDefaultAsync();

            viewModel.AverageProgress = averages?.AverageProgress ?? 0;
            viewModel.AverageTimeSpent = averages?.AverageTimeSpent ?? 0;

            // Get learner statistics
            viewModel.TotalLearners = await _context.LessonProgresses
                .Select(lp => lp.UserId)
                .Distinct()
                .CountAsync();

            viewModel.ActiveLearners = await _context.LessonProgresses
                .Where(lp => lp.UpdatedAt >= DateTime.Now.AddDays(-30))
                .Select(lp => lp.UserId)
                .Distinct()
                .CountAsync();

            // Get top learners
            var topLearners = await _context.LessonProgresses
                .GroupBy(lp => lp.UserId)
                .Select(g => new TopLearnerData
                {
                    UserId = g.Key,
                    CompletedLessons = g.Count(lp => lp.ProgressPercentage >= 100),
                    TotalLessons = g.Count(),
                    AverageProgress = g.Average(lp => lp.ProgressPercentage),
                    TotalTimeSpent = g.Sum(lp => lp.TimeSpent ?? 0) / 60, // Convert minutes to hours
                    LastActivity = g.Max(lp => lp.UpdatedAt ?? lp.CreatedAt)
                })
                .OrderByDescending(x => x.CompletedLessons)
                .Take(10)
                .ToListAsync();

            // Get user details for top learners
            var topLearnerIds = topLearners.Select(x => x.UserId).ToList();
            var users = await _userManager.Users
                .Where(u => topLearnerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.UserName, u.Email });

            foreach (var learner in topLearners)
            {
                if (users.TryGetValue(learner.UserId, out var user))
                {
                    learner.UserName = user.UserName ?? "Unknown";
                    learner.UserEmail = user.Email ?? "Unknown";
                }
            }

            viewModel.TopLearners = topLearners;

            // Get top lessons
            viewModel.TopLessons = await _context.LessonProgresses
                .Include(lp => lp.Lesson)
                .ThenInclude(l => l.Chapter)
                .ThenInclude(c => c.Course)
                .GroupBy(lp => lp.LessonId)
                .Select(g => new TopProgressLessonData
                {
                    LessonId = g.Key,
                    LessonTitle = g.First().Lesson.Title,
                    CourseTitle = g.First().Lesson.Chapter.Course.Title,
                    TotalLearners = g.Select(lp => lp.UserId).Distinct().Count(),
                    CompletedLearners = g.Where(lp => lp.ProgressPercentage >= 100)
                                       .Select(lp => lp.UserId)
                                       .Distinct()
                                       .Count(),
                    CompletedCount = g.Count(lp => lp.ProgressPercentage >= 100),
                    AverageProgress = g.Average(lp => lp.ProgressPercentage),
                    AverageTimeSpent = g.Average(lp => lp.TimeSpent ?? 0)
                })
                .OrderByDescending(x => x.CompletedCount)
                .Take(10)
                .ToListAsync();

            // Get recent progresses
            viewModel.RecentProgresses = await _context.LessonProgresses
                .Include(lp => lp.User)
                .Include(lp => lp.Lesson)
                .OrderByDescending(lp => lp.UpdatedAt ?? lp.CreatedAt)
                .Take(10)
                .Select(lp => new RecentProgressData
                {
                    Id = lp.Id,
                    UserName = lp.User.UserName ?? "Unknown",
                    LessonId = lp.LessonId,
                    LessonTitle = lp.Lesson.Title,
                    ProgressPercentage = lp.ProgressPercentage,
                    UpdatedAt = lp.UpdatedAt ?? lp.CreatedAt,
                    Status = lp.Status ?? "Not Started"
                })
                .ToListAsync();

            // Get progress distribution
            var progressGroups = await _context.LessonProgresses
                .Select(lp => lp.ProgressPercentage)
                .ToListAsync();

            var distribution = progressGroups
                .GroupBy(p => GetProgressRange(p))
                .Select(g => new ProgressDistributionData
                {
                    Range = g.Key,
                    Count = g.Count(),
                    Percentage = (float)g.Count() / viewModel.TotalProgresses * 100
                })
                .ToList();

            viewModel.ProgressDistribution = distribution;

            // Get course progress summaries
            viewModel.CourseProgressSummaries = await _context.LessonProgresses
                .Include(lp => lp.Lesson)
                .ThenInclude(l => l.Chapter)
                .ThenInclude(c => c.Course)
                .GroupBy(lp => new { lp.Lesson.Chapter.CourseId, lp.Lesson.Chapter.Course.Title })
                .Select(g => new CourseProgressSummary
                {
                    CourseId = g.Key.CourseId,
                    CourseTitle = g.Key.Title,
                    TotalLearners = g.Select(lp => lp.UserId).Distinct().Count(),
                    TotalLessons = g.Count(),
                    CompletedLearners = g.Where(lp => lp.ProgressPercentage >= 100)
                                       .Select(lp => lp.UserId)
                                       .Distinct()
                                       .Count(),
                    AverageProgress = g.Average(lp => lp.ProgressPercentage)
                })
                .OrderByDescending(x => x.CompletedLearners)
                .Take(10)
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdateProgressRequest request)
            {
                var progress = await _context.LessonProgresses.FindAsync(id);
                if (progress == null)
                {
                return NotFound();
                }

                progress.ProgressPercentage = request.ProgressPercentage;
                progress.UpdatedAt = DateTime.Now;

            if (progress.ProgressPercentage >= 100)
                {
                    progress.Status = "Completed";
                }
            else if (progress.ProgressPercentage > 0)
                {
                    progress.Status = "In Progress";
                }
                else
                {
                    progress.Status = "Not Started";
                }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "L�-i khi cập nhật tiến đ�T" });
            }
        }

        public class UpdateProgressRequest
        {
            public float ProgressPercentage { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateStatusRequest request)
        {
            if (request.Ids == null || !request.Ids.Any() || string.IsNullOrEmpty(request.Status))
            {
                return BadRequest(new { success = false, message = "Dữ li�?u không hợp l�?" });
            }

            try
            {
                var progresses = await _context.LessonProgresses
                    .Where(lp => request.Ids.Contains(lp.Id))
                    .ToListAsync();

                foreach (var progress in progresses)
                {
                    progress.Status = request.Status;
                    progress.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "L�-i khi cập nhật trạng thái" });
            }
        }

        public class BulkUpdateStatusRequest
        {
            public List<int> Ids { get; set; } = new List<int>();
            public string Status { get; set; } = "";
        }

        private string GetProgressRange(float progress)
        {
            if (progress == 0) return "0%";
            if (progress > 0 && progress < 25) return "1-24%";
            if (progress >= 25 && progress < 50) return "25-49%";
            if (progress >= 50 && progress < 75) return "50-74%";
            if (progress >= 75 && progress < 100) return "75-99%";
            return "100%";
        }
    }
}
