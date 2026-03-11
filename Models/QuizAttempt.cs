using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class QuizAttempt
    {
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? SubmittedAt { get; set; }

        public int TotalQuestions { get; set; }

        public int CorrectAnswers { get; set; }

        public float ScorePercent { get; set; }

        public bool Passed { get; set; }

        [ForeignKey("QuizId")]
        public virtual Quiz? Quiz { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<QuizAttemptAnswer> AttemptAnswers { get; set; } = new List<QuizAttemptAnswer>();
    }
}
