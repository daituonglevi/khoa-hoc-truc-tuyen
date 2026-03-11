using System.ComponentModel.DataAnnotations;

namespace ELearningWebsite.Areas.Admin.ViewModels
{
    public class LessonOption
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CommentCount { get; set; }
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string Status { get; set; } = string.Empty;
    }
} 