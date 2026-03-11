using System.ComponentModel.DataAnnotations;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.ViewModels
{
    public class ChapterIndexViewModel
    {
        public List<ChapterListItem> Chapters { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public string Status { get; set; } = string.Empty; // all, active, inactive
        public List<ChapterCourseOption> AvailableCourses { get; set; } = new();
    }

    public class ChapterListItem
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CreateBy { get; set; }
        public int? UpdateBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string? UpdatedByName { get; set; }
        public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        public int LessonsCount { get; set; }
    }

    public class ChapterDetailsViewModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public double CoursePrice { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CreateBy { get; set; }
        public int? UpdateBy { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string? UpdatedByName { get; set; }
        public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        public List<LessonSummary> Lessons { get; set; } = new();
        public int TotalLessons => Lessons.Count;
        public int ActiveLessons => Lessons.Count(l => l.IsActive);
    }

    public class ChapterCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chương")]
        [StringLength(200, ErrorMessage = "Tên chương không được vượt quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public string Status { get; set; } = "Active";

        public List<ChapterCourseOption> AvailableCourses { get; set; } = new();

        // For display purposes
        public string CourseName { get; set; } = string.Empty;
        public double CoursePrice { get; set; }
    }

    public class ChapterEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên chương")]
        [StringLength(200, ErrorMessage = "Tên chương không được vượt quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        public string Status { get; set; } = "Active";

        public List<ChapterCourseOption> AvailableCourses { get; set; } = new();

        // For display purposes
        public string CourseName { get; set; } = string.Empty;
        public double CoursePrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class ChapterDeleteViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int LessonsCount { get; set; }
        public bool HasLessons => LessonsCount > 0;
        public List<LessonSummary> Lessons { get; set; } = new();
    }

    public class ChapterStatisticsViewModel
    {
        public int TotalChapters { get; set; }
        public int ActiveChapters { get; set; }
        public int InactiveChapters { get; set; }
        public int ChaptersThisMonth { get; set; }
        public int ChaptersThisYear { get; set; }
        public int TotalLessons { get; set; }
        public double AverageLessonsPerChapter { get; set; }
        public List<ChapterTrendData> TrendData { get; set; } = new();
        public List<TopCourseChapterData> TopCourses { get; set; } = new();
        public List<RecentChapterData> RecentChapters { get; set; } = new();
        public List<ChapterStatusData> StatusDistribution { get; set; } = new();
    }

    public class ChapterTrendData
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class TopCourseChapterData
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int ChapterCount { get; set; }
        public int LessonCount { get; set; }
        public double CoursePrice { get; set; }
    }

    public class RecentChapterData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class ChapterStatusData
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    // Helper classes
    public class ChapterCourseOption
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public double Price { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ChapterCount { get; set; }
    }

    public class LessonSummary
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Video, Quiz, Document
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive => Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        public int Duration { get; set; } // in minutes
    }
}
