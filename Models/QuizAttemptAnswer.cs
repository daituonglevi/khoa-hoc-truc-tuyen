using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class QuizAttemptAnswer
    {
        public int Id { get; set; }

        [Required]
        public int AttemptId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        public int? SelectedAnswerId { get; set; }

        public bool IsCorrect { get; set; }

        [ForeignKey("AttemptId")]
        public virtual QuizAttempt? Attempt { get; set; }

        [ForeignKey("QuestionId")]
        public virtual QuizQuestion? Question { get; set; }

        [ForeignKey("SelectedAnswerId")]
        public virtual QuizAnswer? SelectedAnswer { get; set; }
    }
}
