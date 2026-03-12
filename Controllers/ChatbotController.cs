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
        public async Task<IActionResult> Ask([FromBody] ChatbotAskRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var trimmedMessage = request.Message.Trim();
            var context = "";  // Không giới hạn context - chatbot trả lời bất kỳ câu hỏi gì

            var sessionKey = $"chatbot_history_{userId}";
            var history = GetHistory(sessionKey);

            try
            {
                var answer = await _chatbotService.AskAsync(trimmedMessage, context, history, cancellationToken);

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
    }
}
