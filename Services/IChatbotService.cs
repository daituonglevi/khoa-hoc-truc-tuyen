namespace ELearningWebsite.Services
{
    public interface IChatbotService
    {
        Task<string> AskAsync(
            string userMessage,
            string context,
            string? imageDataUrl,
            IReadOnlyList<(string Role, string Content)> history,
            CancellationToken cancellationToken = default);
    }
}
