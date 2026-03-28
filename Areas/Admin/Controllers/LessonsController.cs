using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Models;
using Microsoft.AspNetCore.Authorization;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Instructor")]
    public class LessonsController : Controller
    {
        private readonly ELearningWebsite.Data.ApplicationDbContext _context;
        public LessonsController(ELearningWebsite.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Lessons
        public IActionResult Index(int? courseId, int? chapterId, string? lessonKeyword)
        {
            var courses = _context.Courses
                .OrderBy(c => c.Title)
                .Select(c => new { c.Id, Title = c.Title ?? string.Empty })
                .ToList();

            var chaptersQuery = _context.Chapters.AsQueryable();
            if (courseId.HasValue)
            {
                chaptersQuery = chaptersQuery.Where(ch => ch.CourseId == courseId.Value);
            }

            var chapters = chaptersQuery
                .OrderBy(ch => ch.Name)
                .Select(ch => new { ch.Id, ch.Name, ch.CourseId })
                .ToList();

            var lessonsQuery = _context.Set<Lesson>()
                .Include(l => l.Chapter)
                    .ThenInclude(ch => ch!.Course)
                .Include(l => l.Quiz)
                .AsQueryable();

            if (courseId.HasValue)
            {
                lessonsQuery = lessonsQuery.Where(l => l.Chapter != null && l.Chapter.CourseId == courseId.Value);
            }

            if (chapterId.HasValue)
            {
                lessonsQuery = lessonsQuery.Where(l => l.ChapterId == chapterId.Value);
            }

            if (!string.IsNullOrWhiteSpace(lessonKeyword))
            {
                var keyword = lessonKeyword.Trim();
                lessonsQuery = lessonsQuery.Where(l => l.Title.Contains(keyword));
            }

            var lessons = lessonsQuery
                .OrderBy(l => l.ChapterId)
                .ThenBy(l => l.OrderIndex)
                .ToList();

            ViewBag.Courses = courses;
            ViewBag.Chapters = chapters;
            ViewBag.SelectedCourseId = courseId;
            ViewBag.SelectedChapterId = chapterId;
            ViewBag.LessonKeyword = lessonKeyword ?? string.Empty;

            return View(lessons);
        }

        // GET: Admin/Lessons/Details/5
        public IActionResult Details(int id)
        {
            var lesson = _context.Lessons
                .Include(l => l.Chapter)
                .Include(l => l.Quiz)
                .FirstOrDefault(l => l.Id == id);

            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }

        // GET: Admin/Lessons/Create
        public IActionResult Create(int? chapterId)
        {
            var chapters = _context.Set<Chapter>().ToList();
            ViewBag.Chapters = chapters;
            var lesson = new Lesson();
            if (chapterId.HasValue)
                lesson.ChapterId = chapterId.Value;
            return View(lesson);
        }

        // POST: Admin/Lessons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Lesson lesson)
        {
            if (ModelState.IsValid)
            {
                lesson.VideoUrl = NormalizeVideoUrlForStorage(lesson.VideoUrl);
                _context.Set<Lesson>().Add(lesson);
                _context.SaveChanges();
                // Sau khi tạo, chuy�fn về trang chi tiết chương
                return RedirectToAction("Details", "Chapters", new { id = lesson.ChapterId });
            }
            // Nếu l�-i, truyền lại danh sách chương
            ViewBag.Chapters = _context.Set<Chapter>().ToList();
            return View(lesson);
        }

        // GET: Admin/Lessons/Edit/5
        public IActionResult Edit(int id)
        {
            var lesson = _context.Lessons.FirstOrDefault(l => l.Id == id);
            if (lesson == null)
            {
                return NotFound();
            }

            ViewBag.Chapters = _context.Set<Chapter>().ToList();
            return View(lesson);
        }

        // POST: Admin/Lessons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Lesson lesson)
        {
            if (id != lesson.Id)
            {
                return NotFound();
            }

            var existingLesson = _context.Lessons.FirstOrDefault(l => l.Id == id);
            if (existingLesson == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existingLesson.ChapterId = lesson.ChapterId;
                existingLesson.Title = lesson.Title;
                existingLesson.Description = lesson.Description;
                existingLesson.Content = lesson.Content;
                existingLesson.VideoUrl = NormalizeVideoUrlForStorage(lesson.VideoUrl);
                existingLesson.Duration = lesson.Duration;
                existingLesson.OrderIndex = lesson.OrderIndex;
                existingLesson.Type = lesson.Type;
                existingLesson.Status = lesson.Status;
                existingLesson.UpdatedAt = DateTime.Now;

                _context.SaveChanges();
                return RedirectToAction("Details", "Chapters", new { id = existingLesson.ChapterId });
            }

            ViewBag.Chapters = _context.Set<Chapter>().ToList();
            return View(lesson);
        }

        private static string? NormalizeVideoUrlForStorage(string? videoUrl)
        {
            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                return videoUrl;
            }

            var rawValue = videoUrl.Trim();
            var decodedValue = System.Net.WebUtility.HtmlDecode(rawValue);

            var iframeSrcMatch = System.Text.RegularExpressions.Regex.Match(
                decodedValue,
                "src\\s*=\\s*['\"](?<src>[^'\"]+)['\"]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            var sourceUrl = iframeSrcMatch.Success
                ? iframeSrcMatch.Groups["src"].Value.Trim()
                : decodedValue;

            if (!sourceUrl.Contains("drive.google.com", StringComparison.OrdinalIgnoreCase))
            {
                return sourceUrl;
            }

            var driveRegex = System.Text.RegularExpressions.Regex.Match(sourceUrl, @"(?:/d/|id=)([a-zA-Z0-9_-]{10,})");
            if (!driveRegex.Success)
            {
                return sourceUrl;
            }

            var driveFileId = driveRegex.Groups[1].Value;
            var driveEmbedUrl = $"https://drive.google.com/file/d/{driveFileId}/preview";
            return $"<iframe src=\"{driveEmbedUrl}\" width=\"640\" height=\"480\"></iframe>";
        }

        // GET: Admin/Lessons/Quiz?lessonId=1
        public IActionResult Quiz(int lessonId)
        {
            var lesson = _context.Lessons
                .Include(l => l.Chapter)
                .FirstOrDefault(l => l.Id == lessonId);

            if (lesson == null)
            {
                return NotFound();
            }

            var quiz = _context.Quizzes
                .Include(q => q.Questions.OrderBy(qq => qq.OrderIndex))
                    .ThenInclude(qq => qq.Answers.OrderBy(a => a.OrderIndex))
                .FirstOrDefault(q => q.LessonId == lessonId);

            ViewBag.Lesson = lesson;
            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateQuiz(int lessonId, string title, int passPercent = 80)
        {
            var lesson = _context.Lessons.Find(lessonId);
            if (lesson == null)
            {
                return NotFound();
            }

            var existing = _context.Quizzes.FirstOrDefault(q => q.LessonId == lessonId);
            if (existing == null)
            {
                existing = new Quiz
                {
                    LessonId = lessonId,
                    Title = string.IsNullOrWhiteSpace(title) ? $"Quiz - {lesson.Title}" : title,
                    PassPercent = Math.Clamp(passPercent, 1, 100),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                _context.Quizzes.Add(existing);
            }
            else
            {
                existing.Title = string.IsNullOrWhiteSpace(title) ? existing.Title : title;
                existing.PassPercent = Math.Clamp(passPercent, 1, 100);
                existing.UpdatedAt = DateTime.Now;
            }

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã lưu bài tập trắc nghiệm.";
            return RedirectToAction(nameof(Quiz), new { lessonId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddQuestion(int quizId, string content, int orderIndex = 1, int score = 1)
        {
            var quiz = _context.Quizzes.FirstOrDefault(q => q.Id == quizId);
            if (quiz == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Nội dung câu hỏi không được để trống.";
                return RedirectToAction(nameof(Quiz), new { lessonId = quiz.LessonId });
            }

            _context.QuizQuestions.Add(new QuizQuestion
            {
                QuizId = quizId,
                Content = content.Trim(),
                OrderIndex = orderIndex,
                Score = Math.Max(1, score),
                CreatedAt = DateTime.Now
            });

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã thêm câu hỏi.";
            return RedirectToAction(nameof(Quiz), new { lessonId = quiz.LessonId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAnswer(int questionId, string answerText, bool isCorrect = false, int orderIndex = 1)
        {
            var question = _context.QuizQuestions
                .Include(q => q.Quiz)
                .FirstOrDefault(q => q.Id == questionId);
            if (question == null || question.Quiz == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(answerText))
            {
                TempData["ErrorMessage"] = "Nội dung đáp án không được để trống.";
                return RedirectToAction(nameof(Quiz), new { lessonId = question.Quiz.LessonId });
            }

            if (isCorrect)
            {
                var oldCorrectAnswers = _context.QuizAnswers.Where(a => a.QuestionId == questionId && a.IsCorrect).ToList();
                foreach (var ans in oldCorrectAnswers)
                {
                    ans.IsCorrect = false;
                }
            }

            _context.QuizAnswers.Add(new QuizAnswer
            {
                QuestionId = questionId,
                AnswerText = answerText.Trim(),
                IsCorrect = isCorrect,
                OrderIndex = orderIndex,
                CreatedAt = DateTime.Now
            });

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã thêm đáp án.";
            return RedirectToAction(nameof(Quiz), new { lessonId = question.Quiz.LessonId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteQuestion(int questionId)
        {
            var question = _context.QuizQuestions
                .Include(q => q.Quiz)
                .FirstOrDefault(q => q.Id == questionId);
            if (question == null || question.Quiz == null)
            {
                return NotFound();
            }

            _context.QuizQuestions.Remove(question);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã xóa câu hỏi.";
            return RedirectToAction(nameof(Quiz), new { lessonId = question.Quiz.LessonId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAnswer(int answerId)
        {
            var answer = _context.QuizAnswers
                .Include(a => a.Question)
                    .ThenInclude(q => q!.Quiz)
                .FirstOrDefault(a => a.Id == answerId);
            if (answer?.Question?.Quiz == null)
            {
                return NotFound();
            }

            _context.QuizAnswers.Remove(answer);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã xóa đáp án.";
            return RedirectToAction(nameof(Quiz), new { lessonId = answer.Question.Quiz.LessonId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetCorrectAnswer(int answerId)
        {
            var answer = _context.QuizAnswers
                .Include(a => a.Question)
                    .ThenInclude(q => q!.Quiz)
                .FirstOrDefault(a => a.Id == answerId);

            if (answer?.Question?.Quiz == null)
            {
                return NotFound();
            }

            var allAnswers = _context.QuizAnswers
                .Where(a => a.QuestionId == answer.QuestionId)
                .ToList();

            foreach (var ans in allAnswers)
            {
                ans.IsCorrect = ans.Id == answerId;
            }

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Đã cập nhật đáp án đúng.";
            return RedirectToAction(nameof(Quiz), new { lessonId = answer.Question.Quiz.LessonId });
        }

    }
}
