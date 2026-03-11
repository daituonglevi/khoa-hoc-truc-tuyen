using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Finance
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public double Revenue { get; set; } // Sử dụng double như trong database

        public double Fee { get; set; } = 0; // Sử dụng double như trong database

        public string? Type { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Required]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
