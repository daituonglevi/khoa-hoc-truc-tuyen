namespace ELearningWebsite.Models.ViewModels
{
    public class EnrollmentViewModel
    {
        public Enrollment Enrollment { get; set; } = null!;
        public ApplicationUser? User { get; set; }
    }
}
