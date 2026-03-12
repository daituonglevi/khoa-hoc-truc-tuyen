namespace ELearningWebsite.Models
{
    public class ChatbotSettings
    {
        public string Provider { get; set; } = "OpenAICompatible";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gpt-4o-mini";
        public double Temperature { get; set; } = 0.3;
        public int MaxTokens { get; set; } = 700;
        public string SystemPrompt { get; set; } =
            "Bạn là trợ lý học tập của LMS VJU. Trả lời ngắn gọn, dễ hiểu bằng tiếng Việt. " +
            "Chỉ sử dụng dữ liệu khóa học được cung cấp trong ngữ cảnh; nếu thiếu dữ liệu, hãy nói rõ không đủ thông tin.";
    }
}
