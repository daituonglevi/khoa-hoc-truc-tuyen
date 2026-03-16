using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Models;
using Microsoft.AspNetCore.Authorization;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LessonsController : Controller
    {
        private readonly ELearningWebsite.Data.ApplicationDbContext _context;
        public LessonsController(ELearningWebsite.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Lessons
        public IActionResult Index()
        {
            var lessons = _context.Set<Lesson>()
                .Include(l => l.Chapter)
                .Include(l => l.Quiz)
                .ToList();
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
                existingLesson.VideoUrl = lesson.VideoUrl;
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
            TempData["SuccessMessage"] = "Da luu bai tap trac nghiem.";
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
                TempData["ErrorMessage"] = "Noi dung cau hoi khong duoc de trong.";
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
            TempData["SuccessMessage"] = "Da them cau hoi.";
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
                TempData["ErrorMessage"] = "Noi dung dap an khong duoc de trong.";
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
            TempData["SuccessMessage"] = "Da them dap an.";
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
            TempData["SuccessMessage"] = "Da xoa cau hoi.";
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
            TempData["SuccessMessage"] = "Da xoa dap an.";
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
            TempData["SuccessMessage"] = "Da cap nhat dap an dung.";
            return RedirectToAction(nameof(Quiz), new { lessonId = answer.Question.Quiz.LessonId });
        }

    }
}
