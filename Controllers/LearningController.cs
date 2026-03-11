using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Models;
using ELearningWebsite.Data;
using Microsoft.AspNetCore.Identity;

namespace ELearningWebsite.Controllers
{
    [Authorize]
    public class LearningController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LearningController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Learning/Course/5
        public async Task<IActionResult> Course(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var isAdmin = User.IsInRole("Admin");
            var course = await _context.Courses
                .Include(c => c.Chapters)
                    .ThenInclude(ch => ch.Lessons)
                        .ThenInclude(l => l.LessonProgresses.Where(lp => lp.UserId == user.Id))
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // Admin có th�f xem trực tiếp không cần enrollment
            if (!isAdmin)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == id && e.UserId == user.Id);

                if (enrollment == null || enrollment.Status != 1) // 1 = Active
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            return View(course);
        }

        // GET: Learning/Lesson/5
        public async Task<IActionResult> Lesson(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var isAdmin = User.IsInRole("Admin");
            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                    .ThenInclude(c => c.Course)
                .Include(l => l.Quiz)
                    .ThenInclude(q => q.Questions.OrderBy(qq => qq.OrderIndex))
                        .ThenInclude(qq => qq.Answers.OrderBy(a => a.OrderIndex))
                .Include(l => l.LessonProgresses.Where(lp => lp.UserId == user.Id))
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            // Admin có th�f xem trực tiếp không cần enrollment
            if (!isAdmin)
            {
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == lesson.Chapter.CourseId && e.UserId == user.Id);

                if (enrollment == null || enrollment.Status != 1)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            // Tạo hoặc cập nhật tiến đ�T học tập
            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.LessonId == id && lp.UserId == user.Id);

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    LessonId = lesson.Id,
                    UserId = user.Id,
                    Status = "In Progress",
                    CreatedAt = DateTime.Now
                };
                _context.Add(progress);
            }

            var chapterLessons = await _context.Lessons
                .Where(l => l.ChapterId == lesson.ChapterId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();

            ViewBag.UserProgress = progress.ProgressPercentage;
            ViewBag.ProgressStatus = progress.Status ?? "In Progress";
            ViewBag.ChapterLessons = chapterLessons;

            await _context.SaveChangesAsync();
            return View(lesson);
        }

        // POST: Learning/SubmitQuiz
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz([FromBody] QuizSubmissionInput input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                .Include(l => l.Quiz)
                    .ThenInclude(q => q.Questions)
                        .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(l => l.Id == input.LessonId);

            if (lesson == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                var enrollmentCheck = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.CourseId == lesson.Chapter.CourseId && e.UserId == user.Id);
                if (enrollmentCheck == null || enrollmentCheck.Status != 1)
                {
                    return Forbid();
                }
            }

            if (lesson.Quiz == null || !lesson.Quiz.IsActive)
            {
                return BadRequest(new { success = false, message = "Bài học chưa có bài tập trắc nghi�?m." });
            }

            var questions = lesson.Quiz.Questions.OrderBy(q => q.OrderIndex).ToList();
            if (!questions.Any())
            {
                return BadRequest(new { success = false, message = "Bài tập chưa có câu hỏi." });
            }

            if (questions.Any(q => !q.Answers.Any()))
            {
                return BadRequest(new { success = false, message = "Có câu hỏi chưa có đáp án. Vui lòng báo quản tr�< viên." });
            }

            if (questions.Any(q => !q.Answers.Any(a => a.IsCorrect)))
            {
                return BadRequest(new { success = false, message = "Có câu hỏi chưa được chọn đáp án đúng. Vui lòng báo quản tr�< viên." });
            }

            var inputMap = input.Answers.ToDictionary(a => a.QuestionId, a => a.AnswerId);
            var correctAnswers = 0;
            var attempt = new QuizAttempt
            {
                QuizId = lesson.Quiz.Id,
                UserId = user.Id,
                StartedAt = DateTime.Now,
                SubmittedAt = DateTime.Now,
                TotalQuestions = questions.Count
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            foreach (var question in questions)
            {
                inputMap.TryGetValue(question.Id, out var selectedAnswerId);
                var isCorrect = selectedAnswerId.HasValue && question.Answers.Any(a => a.Id == selectedAnswerId.Value && a.IsCorrect);
                if (isCorrect)
                {
                    correctAnswers++;
                }

                _context.QuizAttemptAnswers.Add(new QuizAttemptAnswer
                {
                    AttemptId = attempt.Id,
                    QuestionId = question.Id,
                    SelectedAnswerId = selectedAnswerId,
                    IsCorrect = isCorrect
                });
            }

            var scorePercent = (float)correctAnswers * 100 / questions.Count;
            var passed = scorePercent >= lesson.Quiz.PassPercent;

            attempt.CorrectAnswers = correctAnswers;
            attempt.ScorePercent = scorePercent;
            attempt.Passed = passed;

            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.LessonId == lesson.Id && lp.UserId == user.Id);

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    LessonId = lesson.Id,
                    UserId = user.Id,
                    CreatedAt = DateTime.Now
                };
                _context.LessonProgresses.Add(progress);
            }

            progress.HighestMark = progress.HighestMark.HasValue
                ? Math.Max(progress.HighestMark.Value, scorePercent)
                : scorePercent;
            progress.CountDoing = (progress.CountDoing ?? 0) + 1;
            progress.UpdatedAt = DateTime.Now;
            progress.ProgressPercentage = passed ? 100 : Math.Max(progress.ProgressPercentage, scorePercent);
            progress.Status = passed ? "Completed" : "In Progress";

            await _context.SaveChangesAsync();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == lesson.Chapter.CourseId && e.UserId == user.Id);
            if (enrollment != null)
            {
                var totalLessons = await _context.Lessons
                    .Include(l => l.Chapter)
                    .Where(l => l.Chapter.CourseId == lesson.Chapter.CourseId)
                    .CountAsync();

                var completedLessons = await _context.LessonProgresses
                    .Include(lp => lp.Lesson)
                        .ThenInclude(l => l.Chapter)
                    .Where(lp => lp.Lesson.Chapter.CourseId == lesson.Chapter.CourseId
                           && lp.UserId == user.Id
                           && lp.ProgressPercentage >= 100)
                    .CountAsync();

                enrollment.Progress = totalLessons > 0 ? (float)completedLessons / totalLessons * 100 : 0;
                await _context.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                correctAnswers,
                totalQuestions = questions.Count,
                scorePercent,
                passed,
                message = passed
                    ? "Chuc mung ban da vuot qua bai kiem tra"
                    : "Ban can phai on ky lai kien thuc da hoc"
            });
        }

        // POST: Learning/UpdateProgress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProgress(int lessonId, float progressPercentage, float timeSpent)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.LessonId == lessonId && lp.UserId == user.Id);

            if (progress == null)
            {
                return NotFound();
            }

            progress.ProgressPercentage = progressPercentage;
            progress.TimeSpent = timeSpent;
            progress.UpdatedAt = DateTime.Now;
            progress.Status = progressPercentage >= 100 ? "Completed" : "In Progress";

            if (progress.CountDoing == null)
            {
                progress.CountDoing = 1;
            }
            else
            {
                progress.CountDoing++;
            }

            _context.Update(progress);
            await _context.SaveChangesAsync();

            // Cập nhật tiến đ�T khóa học
            var lesson = await _context.Lessons
                .Include(l => l.Chapter)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == lesson.Chapter.CourseId && e.UserId == user.Id);

            if (enrollment != null)
            {
                var totalLessons = await _context.Lessons
                    .Include(l => l.Chapter)
                    .Where(l => l.Chapter.CourseId == lesson.Chapter.CourseId)
                    .CountAsync();

                var completedLessons = await _context.LessonProgresses
                    .Include(lp => lp.Lesson)
                    .ThenInclude(l => l.Chapter)
                    .Where(lp => lp.Lesson.Chapter.CourseId == lesson.Chapter.CourseId 
                           && lp.UserId == user.Id 
                           && lp.ProgressPercentage >= 100)
                    .CountAsync();

                enrollment.Progress = totalLessons > 0 ? (float)completedLessons / totalLessons * 100 : 0;
                _context.Update(enrollment);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // POST: Learning/UpdateQuizProgress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuizProgress(int lessonId, float mark)
        {
            var user = await _userManager.GetUserAsync(User);
            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.LessonId == lessonId && lp.UserId == user.Id);

            if (progress == null)
            {
                return NotFound();
            }

            if (progress.HighestMark == null || mark > progress.HighestMark)
            {
                progress.HighestMark = mark;
            }

            progress.ProgressPercentage = mark;
            progress.UpdatedAt = DateTime.Now;
            progress.Status = mark >= 80 ? "Completed" : "In Progress"; // 80% là đi�fm đạt
            progress.CountDoing = (progress.CountDoing ?? 0) + 1;

            _context.Update(progress);
            await _context.SaveChangesAsync();

            return Json(new { success = true, highestMark = progress.HighestMark });
        }

        // GET: Learning/ContinueLearning
        public async Task<IActionResult> ContinueLearning()
        {
            var user = await _userManager.GetUserAsync(User);
            
            // Lấy khóa học đang học gần đây nhất
            var lastProgress = await _context.LessonProgresses
                .Include(lp => lp.Lesson)
                    .ThenInclude(l => l.Chapter)
                        .ThenInclude(c => c.Course)
                .Where(lp => lp.UserId == user.Id)
                .OrderByDescending(lp => lp.UpdatedAt ?? lp.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastProgress == null)
            {
                // Nếu chưa có tiến đ�T học tập, chuy�fn về trang khóa học của học viên
                return RedirectToAction("MyLearning", "Student");
            }

            // Chuy�fn đến bài học cuđi cùng đã học
            return RedirectToAction("Lesson", new { id = lastProgress.LessonId });
        }
    }

    public class QuizSubmissionInput
    {
        public int LessonId { get; set; }
        public List<QuizSubmissionAnswerInput> Answers { get; set; } = new();
    }

    public class QuizSubmissionAnswerInput
    {
        public int QuestionId { get; set; }
        public int? AnswerId { get; set; }
    }
} 