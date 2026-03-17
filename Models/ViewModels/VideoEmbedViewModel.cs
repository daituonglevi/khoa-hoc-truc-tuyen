namespace ELearningWebsite.Models.ViewModels
{
    public class VideoEmbedViewModel
    {
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string IframeClass { get; set; } = "w-100 rounded";
        public string VideoClass { get; set; } = "w-100 rounded";
        public string? IframeId { get; set; }
        public string? VideoId { get; set; }
    }
}
