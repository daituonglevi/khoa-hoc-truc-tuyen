using System.Security.Claims;
using System.Text;
using System.Text.Json;
using ELearningWebsite.Data;
using ELearningWebsite.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ELearningWebsite.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/chatbot")]
    public class ChatbotController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IChatbotService _chatbotService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            ApplicationDbContext dbContext,
            IChatbotService chatbotService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<ChatbotController> logger)
        {
            _dbContext = dbContext;
            _chatbotService = chatbotService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [HttpPost("ask")]
        [RequestFormLimits(MultipartBodyLengthLimit = 5_000_000)]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> Ask([FromForm] ChatbotAskRequest request, CancellationToken cancellationToken)
        {
            if (request == null || (string.IsNullOrWhiteSpace(request.Message) && request.Image == null))
            {
                return BadRequest(new { error = "Vui lòng nhập câu hỏi hoặc tải ảnh lên." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var trimmedMessage = (request.Message ?? string.Empty).Trim();
            var imageDataUrl = await BuildImageDataUrlAsync(request.Image, cancellationToken);
            if (string.IsNullOrWhiteSpace(trimmedMessage) && !string.IsNullOrWhiteSpace(imageDataUrl))
            {
                trimmedMessage = "Hãy phân tích ảnh này và trả lời ngắn gọn bằng tiếng Việt.";
            }

            if (string.IsNullOrWhiteSpace(trimmedMessage))
            {
                return BadRequest(new { error = "Câu hỏi không hợp lệ." });
            }

            var context = await BuildWebsiteContextAsync(request, cancellationToken);

            var sessionKey = $"chatbot_history_{userId}";
            var history = GetHistory(sessionKey);
            var aiHistory = history
                .Where(h => !string.IsNullOrWhiteSpace(h.Role) && !string.IsNullOrWhiteSpace(h.Content))
                .Select(h => (h.Role!, h.Content!))
                .ToList();
            var imageUrlForHistory = imageDataUrl;
            var userMessageForHistory = !string.IsNullOrWhiteSpace(request.Message)
                ? request.Message.Trim()
                : "Người dùng đã gửi ảnh để hỏi.";

            try
            {
                var answer = await _chatbotService.AskAsync(trimmedMessage, context, imageDataUrl, aiHistory, cancellationToken);

                history.Add(new ChatHistoryItem
                {
                    Role = "user",
                    Content = userMessageForHistory,
                    ImageUrl = imageUrlForHistory
                });
                history.Add(new ChatHistoryItem
                {
                    Role = "assistant",
                    Content = answer
                });
                history = history.TakeLast(12).ToList();
                SaveHistory(sessionKey, history);

                return Ok(new ChatbotAskResponse
                {
                    Answer = answer,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (ChatbotProviderException ex)
            {
                var providerMessage = string.IsNullOrWhiteSpace(ex.ProviderBody)
                    ? "No provider response body."
                    : ex.ProviderBody.Length > 500
                        ? ex.ProviderBody.Substring(0, 500)
                        : ex.ProviderBody;

                _logger.LogError("Chatbot upstream failed with status {StatusCode}: {ProviderMessage}", ex.StatusCode, providerMessage);

                return StatusCode(502, new
                {
                    error = $"Chatbot provider failed (HTTP {ex.StatusCode}).",
                    details = providerMessage
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chatbot ask failed for user {UserId}", userId);
                return StatusCode(500, new
                {
                    error = "Chatbot tạm thời không khả dụng. Vui lòng thử lại sau.",
                });
            }
        }

        [HttpPost("clear")]
        public IActionResult ClearHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var sessionKey = $"chatbot_history_{userId}";
            HttpContext.Session.Remove(sessionKey);
            return Ok(new { message = "Đã xóa lịch sử chat." });
        }

        [HttpGet("history")]
        public IActionResult GetHistoryApi()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var sessionKey = $"chatbot_history_{userId}";
            var history = GetHistory(sessionKey)
                .Select(h => new ChatHistoryResponseItem
                {
                    Role = h.Role ?? string.Empty,
                    Content = h.Content ?? string.Empty,
                    ImageUrl = h.ImageUrl
                })
                .ToList();

            return Ok(new { history });
        }

        private List<ChatHistoryItem> GetHistory(string sessionKey)
        {
            var raw = HttpContext.Session.GetString(sessionKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<ChatHistoryItem>();
            }

            try
            {
                var items = JsonSerializer.Deserialize<List<ChatHistoryItem>>(raw) ?? new List<ChatHistoryItem>();
                return items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Role) &&
                                (!string.IsNullOrWhiteSpace(i.Content) || !string.IsNullOrWhiteSpace(i.ImageUrl)))
                    .ToList();
            }
            catch
            {
                return new List<ChatHistoryItem>();
            }
        }

        private void SaveHistory(string sessionKey, IReadOnlyList<ChatHistoryItem> history)
        {
            var items = history
                .Select(h => new ChatHistoryItem
                {
                    Role = h.Role,
                    Content = h.Content,
                    ImageUrl = h.ImageUrl
                })
                .ToList();

            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(items));
        }

        public class ChatbotAskRequest
        {
            public string Message { get; set; } = string.Empty;
            public int? CourseId { get; set; }
            public string? CurrentUrl { get; set; }
            public string? CurrentPageTitle { get; set; }
            public IFormFile? Image { get; set; }
        }

        public class ChatbotAskResponse
        {
            public string Answer { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }

        private class ChatHistoryItem
        {
            public string? Role { get; set; }
            public string? Content { get; set; }
            public string? ImageUrl { get; set; }
        }

        public class ChatHistoryResponseItem
        {
            public string Role { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string? ImageUrl { get; set; }
        }

        private async Task<string?> BuildImageDataUrlAsync(IFormFile? image, CancellationToken cancellationToken)
        {
            if (image == null)
            {
                return null;
            }

            if (image.Length <= 0)
            {
                throw new InvalidOperationException("Ảnh tải lên không hợp lệ.");
            }

            if (image.Length > 5_000_000)
            {
                throw new InvalidOperationException("Ảnh vượt quá dung lượng cho phép (tối đa 5MB).");
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            var contentType = (image.ContentType ?? string.Empty).ToLowerInvariant();
            if (!allowedTypes.Contains(contentType))
            {
                throw new InvalidOperationException("Định dạng ảnh chưa được hỗ trợ. Vui lòng dùng JPG, PNG, WEBP hoặc GIF.");
            }

            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream, cancellationToken);
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return $"data:{contentType};base64,{base64}";
        }

        private async Task<string> BuildWebsiteContextAsync(ChatbotAskRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var sb = new StringBuilder();
                var baseUrl = $"{Request.Scheme}://{Request.Host}";

                sb.AppendLine("Thông tin hệ thống LMS VJU:");
                sb.AppendLine("- Tên website: LMS VJU");
                sb.AppendLine("- Trang chủ: " + baseUrl + "/");
                sb.AppendLine("- Trang khóa học: " + baseUrl + "/Home/Courses");
                sb.AppendLine("- Trang giới thiệu: " + baseUrl + "/Home/About");
                sb.AppendLine("- Trang liên hệ: " + baseUrl + "/Home/Contact");

                if (!string.IsNullOrWhiteSpace(request.CurrentUrl))
                {
                    sb.AppendLine("Thông tin trang người dùng đang xem:");
                    sb.AppendLine("- URL hiện tại: " + request.CurrentUrl.Trim());
                }

                if (!string.IsNullOrWhiteSpace(request.CurrentPageTitle))
                {
                    sb.AppendLine("- Tiêu đề trang hiện tại: " + request.CurrentPageTitle.Trim());
                }

                var activeCategories = await _dbContext.Categories
                    .AsNoTracking()
                    .Where(c => c.Status == 1)
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .Take(12)
                    .ToListAsync(cancellationToken);

                var publishedCourseCount = await _dbContext.Courses
                    .AsNoTracking()
                    .Where(c => c.Status == "Published")
                    .CountAsync(cancellationToken);

                var latestCourses = await _dbContext.Courses
                    .AsNoTracking()
                    .Where(c => c.Status == "Published")
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new
                    {
                        c.Id,
                        Title = c.Title ?? "Khóa học chưa có tên",
                        c.Price,
                        CategoryName = c.Category.Name
                    })
                    .Take(10)
                    .ToListAsync(cancellationToken);

                sb.AppendLine("Dữ liệu nội bộ website:");
                sb.AppendLine("- Số lượng khóa học đang published: " + publishedCourseCount);

                if (activeCategories.Count > 0)
                {
                    sb.AppendLine("- Danh mục đang hoạt động: " + string.Join(", ", activeCategories));
                }

                if (latestCourses.Count > 0)
                {
                    sb.AppendLine("- Một số khóa học gần đây:");
                    foreach (var course in latestCourses)
                    {
                        var courseUrl = $"{baseUrl}/Home/CourseDetail/{course.Id}";
                        sb.AppendLine($"  * #{course.Id} - {course.Title} | Danh mục: {course.CategoryName} | Giá: {course.Price:N0} VNĐ | Link: {courseUrl}");
                    }
                }

                sb.AppendLine("Nguyên tắc trả lời:");
                sb.AppendLine("- Nếu người dùng hỏi liên quan LMS VJU, ưu tiên trả lời theo dữ liệu website ở trên.");
                sb.AppendLine("- Nếu người dùng hỏi chung ngoài LMS VJU, vẫn trả lời bình thường.");
                sb.AppendLine("- Không tự bịa link hoặc dữ liệu không có trong ngữ cảnh.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể build website context cho chatbot");
                return "Thông tin website LMS VJU tạm thời chưa tải được. Vẫn có thể trả lời câu hỏi chung bằng tiếng Việt.";
            }
        }
    }
}
