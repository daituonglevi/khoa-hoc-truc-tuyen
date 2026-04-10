namespace ELearningWebsite.Models
{
    public class ChatbotSettings
    {
        public string Provider { get; set; } = "OpenAICompatible";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public string? SiteUrl { get; set; }
        public string SiteName { get; set; } = "LMS VJU";
        public double Temperature { get; set; } = 0.3;
        public int MaxTokens { get; set; } = 700;
        public string SystemPrompt { get; set; } =
            "Bạn là trợ lý AI của website LMS VJU. Trả lời ngắn gọn, chính xác, dễ hiểu bằng tiếng Việt. " +
            "Ưu tiên tận dụng ngữ cảnh website nội bộ được cung cấp (trang hiện tại, khóa học, chuyên mục, thông tin hệ thống). " +
            "Nếu câu hỏi không liên quan LMS VJU, vẫn hỗ trợ trả lời như một trợ lý AI thông thường.";
    }
}
