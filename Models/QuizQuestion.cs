using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELearningWebsite.Models
{
    public class QuizQuestion
    {
        public int Id { get; set; }

        [Required]
        public int QuizId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int OrderIndex { get; set; } = 1;

        public int Score { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("QuizId")]
        public virtual Quiz? Quiz { get; set; }

        public virtual ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();
    }
}
