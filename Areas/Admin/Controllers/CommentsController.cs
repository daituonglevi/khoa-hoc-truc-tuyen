using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using ELearningWebsite.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Instructor")]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CommentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Comments
        public async Task<IActionResult> Index(int page = 1, string searchTerm = "", int? lessonId = null, int? userId = null, string status = "")
        {
            var viewModel = new CommentIndexViewModel
            {
                CurrentPage = page,
                SearchTerm = searchTerm,
                LessonId = lessonId,
                UserId = userId,
                Status = status
            };

            // Build query
            var query = _context.Comments.AsQueryable();
            var currentUserId = GetCurrentUserId();
            if (!IsAdmin())
            {
                if (!currentUserId.HasValue)
                {
                    return Forbid();
                }

                var managedLessonIds = GetManagedLessonIds(currentUserId.Value);
                query = query.Where(c => managedLessonIds.Contains(c.LessonId));
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Content.Contains(searchTerm));
            }

            // Apply lesson filter
            if (lessonId.HasValue)
            {
                query = query.Where(c => c.LessonId == lessonId.Value);
            }

            // Apply user filter
            if (userId.HasValue)
            {
                query = query.Where(c => c.User.Id == userId.Value);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(c => !c.IsDelete);
                        break;
                    case "deleted":
                        query = query.Where(c => c.IsDelete);
                        break;
                    // "all" - no filter
                }
            }

            // Get total count
            viewModel.TotalItems = await query.CountAsync();
            viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalItems / viewModel.PageSize);

            // Get paginated results
            var comments = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * viewModel.PageSize)
                .Take(viewModel.PageSize)
                .Select(c => new CommentListItem
                {
                    Id = c.Id,
                    LessonId = c.LessonId,
                    LessonTitle = "Lesson " + c.LessonId, // Simplified since we don't have Lesson table
                    UserId = c.User.Id,
                    UserName = c.User.UserName,
                    UserEmail = c.User.Email,
                    ParentCommentId = c.ParentCommentId,
                    ParentCommentContent = c.ParentComment != null ? c.ParentComment.Content : "",
                    Content = c.Content ?? "",
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsDelete = c.IsDelete,
                    RepliesCount = c.Replies.Count(r => !r.IsDelete),
                    IsReply = c.ParentCommentId.HasValue
                })
                .ToListAsync();

            viewModel.Comments = comments;

            // Load available lessons and users for filters
            await LoadFilterOptions(viewModel);

            return View(viewModel);
        }

        // GET: Admin/Comments/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (!await CanManageCommentAsync(id))
            {
                return Forbid();
            }

            var comment = await _context.Comments
                .Include(c => c.ParentComment)
                .Include(c => c.Replies.Where(r => !r.IsDelete))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bình luận";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CommentDetailsViewModel
            {
                Id = comment.Id,
                LessonId = comment.LessonId,
                LessonTitle = "Lesson " + comment.LessonId, // Simplified
                CourseTitle = "Course Title", // Simplified
                UserId = comment.User.Id,
                UserName =  comment.User.UserName ,
                UserEmail = comment.User.Email,
                UserAvatar = string.Empty,
                ParentCommentId = comment.ParentCommentId,
                ParentCommentContent = comment.ParentComment?.Content ?? "",
                ParentUserName = comment.ParentComment?.User.UserName ,
                Content = comment.Content ?? "",
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsDelete = comment.IsDelete,
                Replies = comment.Replies.Select(r => new CommentReply
                {
                    Id = r.Id,
                    UserId = r.User.Id,
                    UserName = r.User.UserName ,
                    UserEmail = r.User.Email,
                    Content = r.Content ?? "",
                    CreatedAt = r.CreatedAt,
                    IsDelete = r.IsDelete
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Admin/Comments/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (!await CanManageCommentAsync(id))
            {
                return Forbid();
            }

            var comment = await _context.Comments
                .Include(c => c.ParentComment)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bình luận";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CommentEditViewModel
            {
                Id = comment.Id,
                Content = comment.Content ?? "",
                LessonId = comment.LessonId,
                LessonTitle = "Lesson " + comment.LessonId, // Simplified
                UserId = comment.User.Id,
                UserName = comment.User.UserName,
                ParentCommentId = comment.ParentCommentId,
                ParentCommentContent = comment.ParentComment?.Content ?? "",
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsDelete = comment.IsDelete
            };

            return View(viewModel);
        }

        // POST: Admin/Comments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommentEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!await CanManageCommentAsync(id))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var comment = await _context.Comments.FindAsync(id);
                    if (comment == null)
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy bình luận";
                        return RedirectToAction(nameof(Index));
                    }

                    comment.Content = model.Content;
                    comment.UpdatedAt = DateTime.Now;
                    comment.IsDelete = model.IsDelete;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật bình luận thành công";
                    return RedirectToAction(nameof(Details), new { id = comment.Id });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                }
            }

            // If we got this far, something failed, redisplay form
            model.LessonTitle = "Lesson " + model.LessonId;
            model.UserName = model.UserId != null ? "User " + model.UserId : "Anonymous";
            return View(model);
        }

        // GET: Admin/Comments/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanManageCommentAsync(id))
            {
                return Forbid();
            }

            var comment = await _context.Comments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bình luận";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CommentDeleteViewModel
            {
                Id = comment.Id,
                Content = comment.Content ?? "",
                LessonTitle = "Lesson " + comment.LessonId,
                UserName = comment.User.UserName,
                CreatedAt = comment.CreatedAt,
                RepliesCount = comment.Replies.Count(r => !r.IsDelete)
            };

            return View(viewModel);
        }

        // POST: Admin/Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                if (!await CanManageCommentAsync(id))
                {
                    return Forbid();
                }

                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy bình luận";
                    return RedirectToAction(nameof(Index));
                }

                comment.IsDelete = true;
                comment.UpdatedAt = DateTime.Now;

                // Also mark all replies as deleted
                var replies = await _context.Comments
                    .Where(c => c.ParentCommentId == id)
                    .ToListAsync();

                foreach (var reply in replies)
                {
                    reply.IsDelete = true;
                    reply.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa bình luận thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có l�-i xảy ra: " + ex.Message;
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task LoadFilterOptions(CommentIndexViewModel viewModel)
        {
            var currentUserId = GetCurrentUserId();
            var commentsQuery = _context.Comments.AsQueryable();
            if (!IsAdmin())
            {
                if (!currentUserId.HasValue)
                {
                    viewModel.AvailableLessons = new List<LessonOption>();
                    viewModel.AvailableUsers = new List<UserOption>();
                    return;
                }

                var managedLessonIds = GetManagedLessonIds(currentUserId.Value);
                commentsQuery = commentsQuery.Where(c => managedLessonIds.Contains(c.LessonId));
            }

            // Load available lessons (simplified)
            viewModel.AvailableLessons = await commentsQuery
                .Select(c => new LessonOption
                {
                    Id = c.LessonId,
                    Title = "Lesson " + c.LessonId,
                    CommentCount = commentsQuery.Count(x => x.LessonId == c.LessonId)
                })
                .Distinct()
                .OrderBy(l => l.Id)
                .ToListAsync();

            // Load available users (simplified)
            viewModel.AvailableUsers = await commentsQuery
                .Where(c => c.User.Id > 0)
                .Select(c => new UserOption
                {
                    Id = c.User.Id,
                    UserName = "User " + c.User.UserName,
                    Email = "user" + c.User.Email
                })
                .Distinct()
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        // GET: Admin/Comments/Statistics
        public async Task<IActionResult> Statistics()
        {
            var currentUserId = GetCurrentUserId();
            var commentsQuery = _context.Comments.AsQueryable();
            if (!IsAdmin())
            {
                if (!currentUserId.HasValue)
                {
                    return Forbid();
                }

                var managedLessonIds = GetManagedLessonIds(currentUserId.Value);
                commentsQuery = commentsQuery.Where(c => managedLessonIds.Contains(c.LessonId));
            }

            var now = DateTime.Now;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var stats = new CommentStatisticsViewModel
            {
                TotalComments = await commentsQuery.CountAsync(),
                ActiveComments = await commentsQuery.CountAsync(c => !c.IsDelete),
                DeletedComments = await commentsQuery.CountAsync(c => c.IsDelete),
                TotalReplies = await commentsQuery.CountAsync(c => c.ParentCommentId.HasValue && !c.IsDelete),
                CommentsToday = await commentsQuery.CountAsync(c => c.CreatedAt.Date == today && !c.IsDelete),
                CommentsThisWeek = await commentsQuery.CountAsync(c => c.CreatedAt.Date >= weekStart && !c.IsDelete),
                CommentsThisMonth = await commentsQuery.CountAsync(c => c.CreatedAt.Date >= monthStart && !c.IsDelete)
            };

            // Get trend data for last 7 days
            var trendData = new List<CommentTrendData>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var count = await commentsQuery.CountAsync(c => c.CreatedAt.Date == date && !c.IsDelete);
                trendData.Add(new CommentTrendData { Date = date, Count = count });
            }
            stats.TrendData = trendData;

            // Get top commenters
            var topCommenters = await commentsQuery
                .Where(c => !c.IsDelete && c.User.Id > 0)
                .GroupBy(c => c.User.Id)
                .Select(g => new TopCommenterData
                {
                    UserId = g.Key,
                    UserName = "User " + g.Key,
                    UserEmail = "user" + g.Key + "@example.com",
                    CommentCount = g.Count(),
                    ReplyCount = g.Count(c => c.ParentCommentId.HasValue),
                    LastCommentDate = g.Max(c => c.CreatedAt)
                })
                .OrderByDescending(x => x.CommentCount)
                .Take(10)
                .ToListAsync();

            stats.TopCommenters = topCommenters;

            // Get top lessons by comments
            var topLessons = await commentsQuery
                .Where(c => !c.IsDelete)
                .GroupBy(c => c.LessonId)
                .Select(g => new TopCommentedLessonData
                {
                    LessonId = g.Key,
                    LessonTitle = "Lesson " + g.Key,
                    CourseTitle = "Course Title", // Nếu có th�f join bảng Course thì lấy tên thật
                    CommentCount = g.Count(),
                    ReplyCount = g.Count(c => c.ParentCommentId.HasValue),
                    LastCommentDate = g.Max(c => c.CreatedAt)
                })
                .OrderByDescending(x => x.CommentCount)
                .Take(10)
                .ToListAsync();

            stats.TopLessons = topLessons;

            return View(stats);
        }

        private int? GetCurrentUserId()
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        private IQueryable<int> GetManagedLessonIds(int currentUserId)
        {
            return _context.Lessons
                .Where(l => l.Chapter != null && l.Chapter.Course != null && l.Chapter.Course.CreateBy == currentUserId)
                .Select(l => l.Id);
        }

        private async Task<bool> CanManageCommentAsync(int commentId)
        {
            if (IsAdmin())
            {
                return true;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            var managedLessonIds = GetManagedLessonIds(currentUserId.Value);
            return await _context.Comments.AnyAsync(c => c.Id == commentId && managedLessonIds.Contains(c.LessonId));
        }
    }
}

