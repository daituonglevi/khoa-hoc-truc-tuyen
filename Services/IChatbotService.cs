namespace ELearningWebsite.Services
{
    public interface IChatbotService
    {
        Task<string> AskAsync(
            string userMessage,
            string context,
            IReadOnlyList<(string Role, string Content)> history,
            CancellationToken cancellationToken = default);
    }
}
