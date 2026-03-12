using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ELearningWebsite.Models;
using Microsoft.Extensions.Options;

namespace ELearningWebsite.Services
{
    public class OpenAIChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly ChatbotSettings _settings;
        private readonly ILogger<OpenAIChatbotService> _logger;

        public OpenAIChatbotService(
            HttpClient httpClient,
            IOptions<ChatbotSettings> settings,
            ILogger<OpenAIChatbotService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<string> AskAsync(
            string userMessage,
            string context,
            IReadOnlyList<(string Role, string Content)> history,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                throw new InvalidOperationException("Chatbot API key is not configured.");
            }

            var baseUrl = (_settings.BaseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new InvalidOperationException("Chatbot BaseUrl is not configured.");
            }

            var messages = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["role"] = "system",
                    ["content"] = _settings.SystemPrompt
                }
            };

            if (!string.IsNullOrWhiteSpace(context))
            {
                messages.Add(new Dictionary<string, string>
                {
                    ["role"] = "system",
                    ["content"] = "Ngữ cảnh dữ liệu nội bộ:\n" + context
                });
            }

            foreach (var (role, content) in history)
            {
                if (!string.IsNullOrWhiteSpace(content) &&
                    (role == "user" || role == "assistant"))
                {
                    messages.Add(new Dictionary<string, string>
                    {
                        ["role"] = role,
                        ["content"] = content
                    });
                }
            }

            messages.Add(new Dictionary<string, string>
            {
                ["role"] = "user",
                ["content"] = userMessage
            });

            var request = new
            {
                model = _settings.Model,
                messages,
                temperature = _settings.Temperature,
                max_tokens = _settings.MaxTokens
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Chatbot provider error {StatusCode}: {Body}", (int)response.StatusCode, responseBody);
                throw new InvalidOperationException("Chatbot provider request failed.");
            }

            try
            {
                using var jsonDocument = JsonDocument.Parse(responseBody);
                var root = jsonDocument.RootElement;
                var answer = root
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(answer))
                {
                    throw new InvalidOperationException("Chatbot provider returned empty answer.");
                }

                return answer.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse chatbot provider response: {Body}", responseBody);
                throw new InvalidOperationException("Chatbot provider response parse failed.");
            }
        }
    }
}
