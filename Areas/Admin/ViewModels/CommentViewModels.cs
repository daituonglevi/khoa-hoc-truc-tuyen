using System.ComponentModel.DataAnnotations;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.ViewModels
{
    public class CommentIndexViewModel
    {
        public List<CommentListItem> Comments { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public int? LessonId { get; set; }
        public int? UserId { get; set; }
        public string Status { get; set; } = string.Empty; // all, active, deleted
        public List<LessonOption> AvailableLessons { get; set; } = new();
        public List<UserOption> AvailableUsers { get; set; } = new();
    }

    public class CommentListItem
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public string ParentCommentContent { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDelete { get; set; }
        public int RepliesCount { get; set; }
        public bool IsReply { get; set; }
    }

    public class CommentDetailsViewModel
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserAvatar { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public string ParentCommentContent { get; set; } = string.Empty;
        public string ParentUserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDelete { get; set; }
        public List<CommentReply> Replies { get; set; } = new();
    }

    public class CommentReply
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsDelete { get; set; }
    }

    public class CommentEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "N�Ti dung bình luận là bắt bu�Tc")]
        [StringLength(2000, ErrorMessage = "N�Ti dung không được vượt quá 2000 ký tự")]
        public string Content { get; set; } = string.Empty;

        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int? ParentCommentId { get; set; }
        public string ParentCommentContent { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDelete { get; set; }
    }

    public class CommentDeleteViewModel
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int RepliesCount { get; set; }
        public bool HasReplies => RepliesCount > 0;
    }

    public class CommentStatisticsViewModel
    {
        public int TotalComments { get; set; }
        public int ActiveComments { get; set; }
        public int DeletedComments { get; set; }
        public int TotalReplies { get; set; }
        public int CommentsToday { get; set; }
        public int CommentsThisWeek { get; set; }
        public int CommentsThisMonth { get; set; }
        public List<CommentTrendData> TrendData { get; set; } = new();
        public List<TopCommenterData> TopCommenters { get; set; } = new();
        public List<TopCommentedLessonData> TopLessons { get; set; } = new();
    }

    public class CommentTrendData
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class TopCommenterData
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int CommentCount { get; set; }
        public int ReplyCount { get; set; }
        public DateTime LastCommentDate { get; set; }
    }

    public class TopCommentedLessonData
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public int CommentCount { get; set; }
        public int ReplyCount { get; set; }
        public DateTime LastCommentDate { get; set; }
    }
}
