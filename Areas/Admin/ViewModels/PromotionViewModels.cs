using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ELearningWebsite.Models;

namespace ELearningWebsite.Areas.Admin.ViewModels
{
    public class PromotionIndexViewModel
    {
        public List<PromotionListItem> Promotions { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = string.Empty;
        public string CourseFilter { get; set; } = string.Empty;
        public List<Course> AvailableCourses { get; set; } = new();
    }

    public class PromotionListItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPer { get; set; }
        public int MaxUses { get; set; }
        public int CurrentUses { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status => GetStatus();
        public int UsagePercentage => MaxUses > 0 ? (CurrentUses * 100 / MaxUses) : 0;

        private string GetStatus()
        {
            if (!IsActive) return "Inactive";
            if (EndDate.HasValue && EndDate < DateTime.Now) return "Expired";
            if (StartDate.HasValue && StartDate > DateTime.Now) return "Scheduled";
            if (CurrentUses >= MaxUses) return "Used Up";
            return "Active";
        }
    }

    public class PromotionCreateViewModel
    {
        [Required(ErrorMessage = "Mã khuyến mãi là bắt bu�Tc")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi không được quá 50 ký tự")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã khuyến mãi ch�? được chứa chữ hoa và sđ")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phần tr�fm giảm giá là bắt bu�Tc")]
        [Range(1, 100, ErrorMessage = "Phần tr�fm giảm giá phải từ 1% đến 100%")]
        public decimal DiscountPer { get; set; }

        [Required(ErrorMessage = "Sđ lần sử dụng tđi đa là bắt bu�Tc")]
        [Range(1, 10000, ErrorMessage = "Sđ lần sử dụng phải từ 1 đến 10,000")]
        public int MaxUses { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        public int CourseId { get; set; }

        public List<Course> AvailableCourses { get; set; } = new();
    }

    public class PromotionEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã khuyến mãi là bắt bu�Tc")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi không được quá 50 ký tự")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã khuyến mãi ch�? được chứa chữ hoa và sđ")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phần tr�fm giảm giá là bắt bu�Tc")]
        [Range(1, 100, ErrorMessage = "Phần tr�fm giảm giá phải từ 1% đến 100%")]
        public decimal DiscountPer { get; set; }

        [Required(ErrorMessage = "Sđ lần sử dụng tđi đa là bắt bu�Tc")]
        [Range(1, 10000, ErrorMessage = "Sđ lần sử dụng phải từ 1 đến 10,000")]
        public int MaxUses { get; set; }

        public int CurrentUses { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        public int CourseId { get; set; }

        public bool IsActive { get; set; }

        public List<Course> AvailableCourses { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class PromotionDetailsViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPer { get; set; }
        public int MaxUses { get; set; }
        public int CurrentUses { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public double CoursePrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CreateBy { get; set; }
        public int? UpdateBy { get; set; }

        public string Status => GetStatus();
        public int UsagePercentage => MaxUses > 0 ? (CurrentUses * 100 / MaxUses) : 0;
        public int RemainingUses => Math.Max(0, MaxUses - CurrentUses);
        public double MaxDiscountAmount => CoursePrice * (double)DiscountPer / 100;

        private string GetStatus()
        {
            if (!IsActive) return "Inactive";
            if (EndDate.HasValue && EndDate < DateTime.Now) return "Expired";
            if (StartDate.HasValue && StartDate > DateTime.Now) return "Scheduled";
            if (CurrentUses >= MaxUses) return "Used Up";
            return "Active";
        }
    }

    public class PromotionDeleteViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountPer { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int CurrentUses { get; set; }
        public bool CanDelete => CurrentUses == 0;
        public string DeleteWarning => CurrentUses > 0
            ? $"Không th�f xóa vì đã có {CurrentUses} lần sử dụng"
            : "Bạn có chắc chắn muđn xóa khuyến mãi này?";
    }
}
