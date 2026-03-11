using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class QuizAnswer
    {
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        public string AnswerText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public int OrderIndex { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("QuestionId")]
        public virtual QuizQuestion? Question { get; set; }
    }
}
