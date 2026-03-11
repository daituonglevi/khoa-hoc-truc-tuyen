using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class Answer
    {
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public string AnswerText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        public int OrderIndex { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties - tạm thời comment vì chưa có bảng Question
        // [ForeignKey("QuestionId")]
        // public virtual Question Question { get; set; } = null!;
    }
}
