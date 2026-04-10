using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
            var currentUserId = GetCurrentUserId();
            if (!IsAdmin() && !currentUserId.HasValue)
            {
                return Forbid();
            }

            var coursesQuery = _context.Courses.AsQueryable();
            if (!IsAdmin())
            {
                coursesQuery = coursesQuery.Where(c => c.CreateBy == currentUserId!.Value);
            }

            var courses = coursesQuery
                .OrderBy(c => c.Title)
                .Select(c => new { c.Id, Title = c.Title ?? string.Empty })
                .ToList();

            var chaptersQuery = _context.Chapters.AsQueryable();
            if (!IsAdmin())
            {
                chaptersQuery = chaptersQuery.Where(ch => ch.Course != null && ch.Course.CreateBy == currentUserId!.Value);
            }

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

            if (!IsAdmin())
            {
                lessonsQuery = lessonsQuery.Where(l => l.Chapter != null && l.Chapter.Course != null && l.Chapter.Course.CreateBy == currentUserId!.Value);
            }

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
            if (!CanManageLesson(id))
            {
                return Forbid();
            }

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
            if (chapterId.HasValue && !CanManageChapter(chapterId.Value))
            {
                return Forbid();
            }

            var chaptersQuery = _context.Set<Chapter>().AsQueryable();
            var currentUserId = GetCurrentUserId();
            if (!IsAdmin())
            {
                if (!currentUserId.HasValue)
                {
                    return Forbid();
                }

                chaptersQuery = chaptersQuery.Where(ch => ch.Course != null && ch.Course.CreateBy == currentUserId.Value);
            }

            var chapters = chaptersQuery.ToList();
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
            if (!CanManageChapter(lesson.ChapterId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Forbid();
                }

                lesson.VideoUrl = NormalizeVideoUrlForStorage(lesson.VideoUrl);
                lesson.CreateBy = currentUserId.Value;
                _context.Set<Lesson>().Add(lesson);
                _context.SaveChanges();
                // Sau khi tạo, chuy�fn về trang chi tiết chương
                return RedirectToAction("Details", "Chapters", new { id = lesson.ChapterId });
            }
            // Nếu l�-i, truyền lại danh sách chương
            var chaptersQuery = _context.Set<Chapter>().AsQueryable();
            var currentUserId2 = GetCurrentUserId();
            if (!IsAdmin() && currentUserId2.HasValue)
            {
                chaptersQuery = chaptersQuery.Where(ch => ch.Course != null && ch.Course.CreateBy == currentUserId2.Value);
            }
            ViewBag.Chapters = chaptersQuery.ToList();
            return View(lesson);
        }

        // GET: Admin/Lessons/Edit/5
        public IActionResult Edit(int id)
        {
            if (!CanManageLesson(id))
            {
                return Forbid();
            }

            var lesson = _context.Lessons.FirstOrDefault(l => l.Id == id);
            if (lesson == null)
            {
                return NotFound();
            }

            var chaptersQuery = _context.Set<Chapter>().AsQueryable();
            var currentUserId = GetCurrentUserId();
            if (!IsAdmin() && currentUserId.HasValue)
            {
                chaptersQuery = chaptersQuery.Where(ch => ch.Course != null && ch.Course.CreateBy == currentUserId.Value);
            }
            ViewBag.Chapters = chaptersQuery.ToList();
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

            if (!CanManageLesson(id) || !CanManageChapter(lesson.ChapterId))
            {
                return Forbid();
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
                var currentUserId = GetCurrentUserId();
                if (currentUserId.HasValue)
                {
                    existingLesson.UpdateBy = currentUserId.Value;
                }

                _context.SaveChanges();
                return RedirectToAction("Details", "Chapters", new { id = existingLesson.ChapterId });
            }

            var chaptersQuery = _context.Set<Chapter>().AsQueryable();
            var currentUserId2 = GetCurrentUserId();
            if (!IsAdmin() && currentUserId2.HasValue)
            {
                chaptersQuery = chaptersQuery.Where(ch => ch.Course != null && ch.Course.CreateBy == currentUserId2.Value);
            }
            ViewBag.Chapters = chaptersQuery.ToList();
            return View(lesson);
        }

        private string? NormalizeVideoUrlForStorage(string? videoUrl)
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

            var mediaOpenMatch = System.Text.RegularExpressions.Regex.Match(
                sourceUrl,
                @"(?:/Admin/MediaLibrary/Open\?id=|/Media/Open\?id=)(\d+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (mediaOpenMatch.Success && int.TryParse(mediaOpenMatch.Groups[1].Value, out var mediaIdFromUrl))
            {
                return Url.Action("Open", "Media", new { area = "", id = mediaIdFromUrl }) ?? sourceUrl;
            }

            if (sourceUrl.Contains(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase)
                && sourceUrl.Contains("/private-media/", StringComparison.OrdinalIgnoreCase))
            {
                var blobName = ExtractBlobNameFromUrl(sourceUrl);
                if (!string.IsNullOrWhiteSpace(blobName))
                {
                    var media = _context.MediaFiles.FirstOrDefault(m => m.Status == "Active" && m.BlobName == blobName);
                    if (media != null)
                    {
                        return Url.Action("Open", "Media", new { area = "", id = media.Id }) ?? sourceUrl;
                    }
                }
            }

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

        private static string? ExtractBlobNameFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                var path = uri.AbsolutePath.Trim('/');
                const string prefix = "private-media/";
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return path.Substring(prefix.Length);
                }
            }

            return null;
        }

        // GET: Admin/Lessons/Quiz?lessonId=1
        public IActionResult Quiz(int lessonId)
        {
            if (!CanManageLesson(lessonId))
            {
                return Forbid();
            }

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
            if (!CanManageLesson(lessonId))
            {
                return Forbid();
            }

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

            if (!CanManageLesson(quiz.LessonId))
            {
                return Forbid();
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

            if (!CanManageLesson(question.Quiz.LessonId))
            {
                return Forbid();
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

            if (!CanManageLesson(question.Quiz.LessonId))
            {
                return Forbid();
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

            if (!CanManageLesson(answer.Question.Quiz.LessonId))
            {
                return Forbid();
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

            if (!CanManageLesson(answer.Question.Quiz.LessonId))
            {
                return Forbid();
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

        private int? GetCurrentUserId()
        {
            var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(rawUserId, out var userId) ? userId : null;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        private bool CanManageCourse(int courseId)
        {
            if (IsAdmin())
            {
                return true;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            return _context.Courses.Any(c => c.Id == courseId && c.CreateBy == currentUserId.Value);
        }

        private bool CanManageChapter(int chapterId)
        {
            if (IsAdmin())
            {
                return true;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            return _context.Chapters.Any(ch => ch.Id == chapterId && ch.Course != null && ch.Course.CreateBy == currentUserId.Value);
        }

        private bool CanManageLesson(int lessonId)
        {
            if (IsAdmin())
            {
                return true;
            }

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return false;
            }

            return _context.Lessons.Any(l => l.Id == lessonId && l.Chapter != null && l.Chapter.Course != null && l.Chapter.Course.CreateBy == currentUserId.Value);
        }

    }
}
