using System.ComponentModel.DataAnnotations;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.ViewModels
{
    public class CertificateIndexViewModel
    {
        public List<CertificateListItem> Certificates { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public int? UserId { get; set; }
        public string Status { get; set; } = string.Empty; // all, issued, pending
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<CourseOption> AvailableCourses { get; set; } = new();
        public List<UserOption> AvailableUsers { get; set; } = new();
    }

    public class CertificateListItem
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string CertificateNumber { get; set; } = string.Empty;
        public string? CertificateUrl { get; set; }
        
        public DateTime IssueDate { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public double Progress { get; set; }
        public int EnrollmentStatus { get; set; }
        public string EnrollmentStatusText { get; set; } = string.Empty;
        public bool HasCertificateFile => !string.IsNullOrEmpty(CertificateUrl);
        public bool IsCompleted => EnrollmentStatus == 3;
    }

    public class CertificateDetailsViewModel
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public double CoursePrice { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string? CertificateUrl { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public double Progress { get; set; }
        public int EnrollmentStatus { get; set; }
        public string EnrollmentStatusText { get; set; } = string.Empty;
        public bool HasCertificateFile => !string.IsNullOrEmpty(CertificateUrl);
        public bool IsCompleted => EnrollmentStatus == 3;
        public DateTime? CompletionDate { get; set; }
        public string CertificateFileName => !string.IsNullOrEmpty(CertificateUrl) ? System.IO.Path.GetFileName(CertificateUrl) : string.Empty;
    }

    public class CertificateCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn đ�fng ký")]
        public int EnrollmentId { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime CompletionDate { get; set; }
        public double Progress { get; set; }
        public int Status { get; set; }
        public DateTime IssueDate { get; set; }
        public string? CertificateNumber { get; set; }
        public string? CertificateUrl { get; set; }

        public List<EnrollmentOption> AvailableEnrollments { get; set; } = new();
    }

    public class CertificateEditViewModel
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sđ chứng ch�?")]
        [StringLength(100, ErrorMessage = "Sđ chứng ch�? không được vượt quá 100 ký tự")]
        public string CertificateNumber { get; set; } = string.Empty;

        [DataType(DataType.DateTime)]
        public DateTime IssueDate { get; set; }

        [Url(ErrorMessage = "URL không hợp l�?")]
        [StringLength(500, ErrorMessage = "URL không được vượt quá 500 ký tự")]
        public string? CertificateUrl { get; set; }

        // For display purposes
        public string UserName { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public double Progress { get; set; }
        public int Status { get; set; }
        public DateTime EnrollmentDate { get; set; }
    }

    public class CertificateDeleteViewModel
    {
        public int Id { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public bool HasCertificateFile { get; set; }
        public string? CertificateUrl { get; set; }
    }

    public class CertificateStatisticsViewModel
    {
        public int TotalCertificates { get; set; }
        public int CertificatesThisMonth { get; set; }
        public int CertificatesThisYear { get; set; }
        public int CertificatesWithFiles { get; set; }
        public int CertificatesWithoutFiles { get; set; }
        public int CompletedEnrollments { get; set; }
        public int PendingCertificates { get; set; }
        public List<CertificateTrendData> TrendData { get; set; } = new();
        public List<TopCourseData> TopCourses { get; set; } = new();
        public List<RecentCertificateData> RecentCertificates { get; set; } = new();
        public double AverageCompletionTime { get; set; }
        public double CertificateCompletionRate { get; set; }
    }

    public class CertificateTrendData
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class TopCourseData
    {
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int CertificateCount { get; set; }
        public double CompletionRate { get; set; }
    }

    public class RecentCertificateData
    {
        public int Id { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
    }

    public class CertificateGenerateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn đ�fng ký")]
        public int EnrollmentId { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime CompletionDate { get; set; }
        public double Progress { get; set; }
        public int Status { get; set; }

        public List<EnrollmentOption> AvailableEnrollments { get; set; } = new();
    }

    // Helper classes
    public class CourseOption
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public double Price { get; set; }
    }

    public class EnrollmentOption
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public double Progress { get; set; }
        public int Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public bool IsCompleted => Status == 3;
        public bool HasCertificate { get; set; }
    }
}
