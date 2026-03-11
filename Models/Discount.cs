using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Discount
    {
        public int Id { get; set; }

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100)]
        public decimal DiscountPer { get; set; }

        [Required]
        public int MaxUses { get; set; }

        public int CurrentUses { get; set; } = 0;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public int CreateBy { get; set; } // Sử dụng int như trong database

        public int? UpdateBy { get; set; }

        [Required]
        public int CourseId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; } = null!;
    }
}
