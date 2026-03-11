using System;
using System.Collections.Generic;

namespace ELearningWebsite.Areas.Admin.ViewModels
{
    public class ReportsIndexViewModel
    {
        // Overall Statistics
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalCategories { get; set; }

        // Revenue Statistics
        public double TotalRevenue { get; set; }
        public double TotalFees { get; set; }
        public double NetRevenue { get; set; }

        // Course Statistics
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public int FreeCourses { get; set; }
        public int PaidCourses { get; set; }

        // User Statistics
        public int VerifiedUsers { get; set; }
        public int UnverifiedUsers { get; set; }

        // Enrollment Statistics
        public int ActiveEnrollments { get; set; }
        public int CompletedEnrollments { get; set; }
        public int SuspendedEnrollments { get; set; }

        // Recent Activities
        public List<RecentActivity> RecentActivities { get; set; } = new();

        // Monthly Trends
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new();

        // Top Courses
        public List<CourseReport> TopCourses { get; set; } = new();

        // Top Categories
        public List<CategoryReport> TopCategories { get; set; } = new();
    }

    public class RecentActivity
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class MonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public int Users { get; set; }
        public int Courses { get; set; }
        public int Enrollments { get; set; }
        public double Revenue { get; set; }
    }

    public class CategoryReport
    {
        public string CategoryName { get; set; } = string.Empty;
        public int CourseCount { get; set; }
        public int PublishedCourses { get; set; }
        public int TotalEnrollments { get; set; }
    }

    public class CourseReport
    {
        public string CourseName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int EnrollmentCount { get; set; }
        public decimal Price { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserReportViewModel
    {
        public int TotalUsers { get; set; }
        public int VerifiedUsers { get; set; }
        public int UnverifiedUsers { get; set; }
        public List<MonthlyUserReport> RegistrationTrend { get; set; } = new();
        public List<UserSummary> RecentUsers { get; set; } = new();
    }

    public class MonthlyUserReport
    {
        public string Month { get; set; } = string.Empty;
        public int NewUsers { get; set; }
    }

    public class UserSummary
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CourseReportViewModel
    {
        public int TotalCourses { get; set; }
        public int PublishedCourses { get; set; }
        public int DraftCourses { get; set; }
        public int FreeCourses { get; set; }
        public int PaidCourses { get; set; }
        public double AveragePrice { get; set; }
        public List<MonthlyCourseReport> CreationTrend { get; set; } = new();
        public List<CategoryReport> CoursesByCategory { get; set; } = new();
    }

    public class MonthlyCourseReport
    {
        public string Month { get; set; } = string.Empty;
        public int NewCourses { get; set; }
    }

    public class FinanceReportViewModel
    {
        public double TotalRevenue { get; set; }
        public double TotalFees { get; set; }
        public double NetRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public double CurrentYearRevenue { get; set; }
        public List<MonthlyFinanceReport> RevenueTrend { get; set; } = new();
    }

    public class MonthlyFinanceReport
    {
        public string Month { get; set; } = string.Empty;
        public double Revenue { get; set; }
        public double Fees { get; set; }
        public double NetRevenue { get; set; }
    }
}