using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.ViewModels
{
    public class MediaLibraryIndexViewModel
    {
        public List<MediaLibraryListItem> Files { get; set; } = new();
        public List<MediaFolderItem> Folders { get; set; } = new();
        public List<MediaFolderBreadcrumbItem> Breadcrumbs { get; set; } = new();
        public List<Course> AvailableCourses { get; set; } = new();
        public int? SelectedCourseId { get; set; }
        public int? SelectedFolderId { get; set; }
        public string CurrentFolderName { get; set; } = "Root";
        public string Search { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
    }

    public class MediaLibraryListItem
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OwnerUserId { get; set; }
        public int? CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int? FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
    }

    public class MediaFolderItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
        public int FileCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MediaFolderBreadcrumbItem
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}