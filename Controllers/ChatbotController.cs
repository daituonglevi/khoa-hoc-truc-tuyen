using System.Security.Claims;
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
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            ApplicationDbContext dbContext,
            IChatbotService chatbotService,
            ILogger<ChatbotController> logger)
        {
            _dbContext = dbContext;
            _chatbotService = chatbotService;
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

            var context = "";  // Không giới hạn context - chatbot trả lời bất kỳ câu hỏi gì

            var sessionKey = $"chatbot_history_{userId}";
            var history = GetHistory(sessionKey);

            try
            {
                var answer = await _chatbotService.AskAsync(trimmedMessage, context, imageDataUrl, history, cancellationToken);

                history.Add(("user", trimmedMessage));
                history.Add(("assistant", answer));
                history = history.TakeLast(12).ToList();
                SaveHistory(sessionKey, history);

                return Ok(new ChatbotAskResponse
                {
                    Answer = answer,
                    Timestamp = DateTime.UtcNow
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
            HttpContext.Session.Remove($"chatbot_history_{userId}");
            return Ok(new { message = "Đã xóa lịch sử chat." });
        }

        private List<(string Role, string Content)> GetHistory(string sessionKey)
        {
            var raw = HttpContext.Session.GetString(sessionKey);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<(string Role, string Content)>();
            }

            try
            {
                var items = JsonSerializer.Deserialize<List<ChatHistoryItem>>(raw) ?? new List<ChatHistoryItem>();
                return items
                    .Where(i => !string.IsNullOrWhiteSpace(i.Role) && !string.IsNullOrWhiteSpace(i.Content))
                    .Select(i => (i.Role!, i.Content!))
                    .ToList();
            }
            catch
            {
                return new List<(string Role, string Content)>();
            }
        }

        private void SaveHistory(string sessionKey, IReadOnlyList<(string Role, string Content)> history)
        {
            var items = history
                .Select(h => new ChatHistoryItem { Role = h.Role, Content = h.Content })
                .ToList();

            HttpContext.Session.SetString(sessionKey, JsonSerializer.Serialize(items));
        }

        public class ChatbotAskRequest
        {
            public string Message { get; set; } = string.Empty;
            public int? CourseId { get; set; }
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
    }
}
