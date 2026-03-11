using System.ComponentModel.DataAnnotations;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.ViewModels
{
    public class LessonProgressIndexViewModel
    {
        public List<LessonProgressListItem> LessonProgresses { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public int? LessonId { get; set; }
        public int? UserId { get; set; }
        public string Status { get; set; } = string.Empty; // all, completed, in-progress, not-started
        public float? MinProgress { get; set; }
        public float? MaxProgress { get; set; }
        public List<LessonOption> AvailableLessons { get; set; } = new();
        public List<UserOption> AvailableUsers { get; set; } = new();

        // Summary statistics
        public int CompletedCount { get; set; }
        public int InProgressCount { get; set; }
        public int NotStartedCount { get; set; }
    }

    public class LessonProgressListItem
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public float ProgressPercentage { get; set; }
        public float? TimeSpent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Status { get; set; }
        public int? Passing { get; set; }
        public int? CountDoing { get; set; }
        public float? HighestMark { get; set; }
        public bool IsCompleted => ProgressPercentage >= 100;
        public string ProgressStatus => Status ?? "Not Started";
        public bool IsPassing => Passing.HasValue && Passing.Value > 0;
        public string ProgressColor => GetProgressColor();
        private string GetProgressColor()
        {
            if (ProgressPercentage >= 100) return "success";
            if (ProgressPercentage >= 75) return "info";
            if (ProgressPercentage >= 50) return "primary";
            if (ProgressPercentage >= 25) return "warning";
            return "danger";
        }
    }

    public class LessonProgressDetailsViewModel
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public float ProgressPercentage { get; set; }
        public float? TimeSpent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Status { get; set; }
        public string ProgressStatus => Status ?? "Not Started";
        public int? Passing { get; set; }
        public bool IsPassing => Passing.HasValue && Passing.Value > 0;
        public int? CountDoing { get; set; }
        public float? HighestMark { get; set; }
        public bool IsCompleted => ProgressPercentage >= 100;
        public List<LessonProgressHistoryItem> ProgressHistory { get; set; } = new();
    }

    public class LessonProgressCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn bài học")]
        public int LessonId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học viên")]
        public int UserId { get; set; }

        [Range(0, 100, ErrorMessage = "Tiến đ�T phải từ 0 đến 100%")]
        public float ProgressPercentage { get; set; } = 0;

        [Range(0, float.MaxValue, ErrorMessage = "Thời gian học phải >= 0")]
        public float? TimeSpent { get; set; }

        public string? Status { get; set; } = "In Progress";

        public int? Passing { get; set; } = 1;

        public int? CountDoing { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Đi�fm cao nhất phải từ 0 đến 100")]
        public float? HighestMark { get; set; }

        public List<LessonOption> AvailableLessons { get; set; } = new();
        public List<UserOption> AvailableUsers { get; set; } = new();
    }

    public class LessonProgressEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bài học")]
        public int LessonId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn học viên")]
        public int UserId { get; set; }

        [Range(0, 100, ErrorMessage = "Tiến đ�T phải từ 0 đến 100%")]
        public float ProgressPercentage { get; set; }

        [Range(0, float.MaxValue, ErrorMessage = "Thời gian học phải >= 0")]
        public float? TimeSpent { get; set; }

        public string? Status { get; set; }

        public int? Passing { get; set; }

        public int? CountDoing { get; set; }

        [Range(0, 100, ErrorMessage = "Đi�fm cao nhất phải từ 0 đến 100")]
        public float? HighestMark { get; set; }

        public List<LessonOption> AvailableLessons { get; set; } = new();
        public List<UserOption> AvailableUsers { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }

    public class LessonProgressDeleteViewModel
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public float ProgressPercentage { get; set; }
        public float? TimeSpent { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Status { get; set; }
        public int? CountDoing { get; set; }
        public float? HighestMark { get; set; }
        public bool IsCompleted => ProgressPercentage >= 100;
    }

    public class LessonProgressStatisticsViewModel
    {
        public int TotalProgresses { get; set; }
        public int CompletedProgresses { get; set; }
        public int NotStartedProgresses { get; set; }
        public int InProgressProgresses { get; set; }
        public int InProgressCount { get; set; }
        public int NotStartedCount { get; set; }
        public float AverageProgress { get; set; }
        public float AverageTimeSpent { get; set; }
        public int TotalLearners { get; set; }
        public int ActiveLearners { get; set; }
        public List<ProgressTrendData> TrendData { get; set; } = new();
        public List<TopLearnerData> TopLearners { get; set; } = new();
        public List<TopProgressLessonData> TopLessons { get; set; } = new();
        public List<RecentProgressData> RecentProgresses { get; set; } = new();
        public List<ProgressDistributionData> ProgressDistribution { get; set; } = new();
        public List<CourseProgressSummary> CourseProgressSummaries { get; set; } = new();
    }

    public class ProgressTrendData
    {
        public DateTime Date { get; set; }
        public float AverageProgress { get; set; }
        public int NewLearners { get; set; }
        public int CompletedCount { get; set; }
        public int StartedCount { get; set; }
    }

    public class TopLearnerData
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public float AverageProgress { get; set; }
        public float TotalTimeSpent { get; set; }
        public DateTime LastActivity { get; set; }
        public float CompletionRate => TotalLessons > 0 ? (float)CompletedLessons / TotalLessons * 100 : 0;
    }

    public class TopProgressLessonData
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public int TotalLearners { get; set; }
        public int CompletedLearners { get; set; }
        public int CompletedCount { get; set; }
        public float AverageProgress { get; set; }
        public float AverageTimeSpent { get; set; }
        public float CompletionRate => TotalLearners > 0 ? (float)CompletedCount / TotalLearners * 100 : 0;
    }

    public class LessonProgressHistoryItem
    {
        public DateTime Date { get; set; }
        public float ProgressPercentage { get; set; }
        public float? TimeSpent { get; set; }
        public string Action { get; set; } = string.Empty; // Started, Updated, Completed
        public string? Notes { get; set; }
    }

    public class RecentProgressData
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public float ProgressPercentage { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsCompleted => ProgressPercentage >= 100;
    }

    public class ProgressDistributionData
    {
        public string Range { get; set; } = string.Empty;
        public int Count { get; set; }
        public float Percentage { get; set; }
    }

    public class CourseProgressSummary
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int TotalLearners { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLearners { get; set; }
        public float AverageProgress { get; set; }
        public float CompletionRate => TotalLearners > 0 ? (float)CompletedLearners / TotalLearners * 100 : 0;
    }

    public class UserOption
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
} 