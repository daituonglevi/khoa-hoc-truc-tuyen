using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Data;
using ELearningWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ELearningWebsite.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CommentController> _logger;

        public CommentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<CommentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Comment/GetComments?lessonId=5
        [HttpGet]
        public async Task<IActionResult> GetComments(int? lessonId, int? courseId)
        {
            try
            {
                var targetLessonId = lessonId ?? courseId;
                if (!targetLessonId.HasValue)
                {
                    return Json(new { success = false, message = "Thiếu lessonId" });
                }

                var allComments = await _context.Comments
                    .Where(c => c.LessonId == targetLessonId.Value && !c.IsDelete)
                    .Include(c => c.User)
                    .Include(c => c.ParentComment)
                        .ThenInclude(p => p!.User)
                    .OrderBy(c => c.CreatedAt)
                    .ToListAsync();

                var modelMap = allComments.ToDictionary(
                    c => c.Id,
                    c => new CommentViewModel
                    {
                        Id = c.Id,
                        Content = c.Content ?? string.Empty,
                        CreatedAt = c.CreatedAt,
                        UserId = c.UserId ?? 0,
                        UserName = c.User?.UserName ?? $"User {c.UserId}",
                        ParentCommentId = c.ParentCommentId,
                        MentionUserName = c.ParentComment?.User?.UserName
                    });

                foreach (var comment in allComments)
                {
                    if (comment.ParentCommentId.HasValue && modelMap.TryGetValue(comment.ParentCommentId.Value, out var parentVm))
                    {
                        parentVm.Replies.Add(modelMap[comment.Id]);
                    }
                }

                foreach (var vm in modelMap.Values)
                {
                    vm.Replies = vm.Replies.OrderBy(r => r.CreatedAt).ToList();
                }

                var commentViewModels = modelMap.Values
                    .Where(c => !c.ParentCommentId.HasValue)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();

                return Json(new { success = true, comments = commentViewModels });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for course {CourseId}", courseId);
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải bình luận" });
            }
        }

        // POST: Comment/PostComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostComment([FromBody] PostCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận" });
                }

                var targetLessonId = request.LessonId ?? request.CourseId;
                if (!targetLessonId.HasValue)
                {
                    return Json(new { success = false, message = "Thiếu lessonId" });
                }

                if (request.ParentCommentId.HasValue)
                {
                    var parentComment = await _context.Comments
                        .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId.Value && !c.IsDelete);

                    if (parentComment == null || parentComment.LessonId != targetLessonId.Value)
                    {
                        return Json(new { success = false, message = "Bình luận cha không hợp lệ" });
                    }
                }

                var comment = new Comment
                {
                    LessonId = targetLessonId.Value,
                    UserId = currentUser.Id,
                    Content = request.Content.Trim(),
                    ParentCommentId = request.ParentCommentId,
                    CreatedAt = DateTime.Now,
                    IsDelete = false
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                var commentViewModel = new CommentViewModel
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    UserId = currentUser.Id,
                    UserName = currentUser.UserName ?? "Unknown User",
                    ParentCommentId = comment.ParentCommentId,
                    MentionUserName = null,
                    Replies = new List<CommentViewModel>()
                };

                return Json(new { success = true, comment = commentViewModel });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting comment");
                return Json(new { success = false, message = "Có lỗi xảy ra khi đăng bình luận" });
            }
        }

        // POST: Comment/DeleteComment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bình luận" });
                }

                // Only allow user to delete their own comments
                if (comment.UserId != currentUser.Id)
                {
                    return Json(new { success = false, message = "Bạn chỉ có thể xóa bình luận của mình" });
                }

                comment.IsDelete = true;
                comment.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Bình luận đã được xóa" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", id);
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa bình luận" });
            }
        }

        private string GetUserName(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            return user?.FullName ?? user?.UserName ?? $"User {userId}";
        }
    }

    // ViewModels
    public class CommentViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public string? MentionUserName { get; set; }
        public List<CommentViewModel> Replies { get; set; } = new();
    }

    public class PostCommentRequest
    {
        [Required(ErrorMessage = "Nội dung bình luận là bắt buộc")]
        [StringLength(2000, ErrorMessage = "Nội dung không được vượt quá 2000 ký tự")]
        public string Content { get; set; } = string.Empty;

        public int? CourseId { get; set; }

        public int? LessonId { get; set; }

        public int? ParentCommentId { get; set; }
    }
}

