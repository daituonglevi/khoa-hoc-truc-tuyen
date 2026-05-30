using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ELearningWebsite.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ELearningWebsite.Services;
using Hangfire;

namespace ELearningWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Instructor")]
    public class LessonsController : Controller
    {
        private readonly ELearningWebsite.Data.ApplicationDbContext _context;
        private readonly IZoomService _zoomService;
        private readonly IEmailSender _emailSender;

        public LessonsController(
            ELearningWebsite.Data.ApplicationDbContext context,
            IZoomService zoomService,
            IEmailSender emailSender)
        {
            _context = context;
            _zoomService = zoomService;
            _emailSender = emailSender;
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
                coursesQuery = coursesQuery.Where(c =>
                    c.CreateBy == currentUserId!.Value
                    || _context.CourseCollaborators.Any(cc =>
                        cc.CourseId == c.Id
                        && cc.UserId == currentUserId.Value
                        && cc.Status == "Active"));
            }

            var courses = coursesQuery
                .OrderBy(c => c.Title)
                .Select(c => new { c.Id, Title = c.Title ?? string.Empty })
                .ToList();

            var chaptersQuery = _context.Chapters.AsQueryable();
            if (!IsAdmin())
            {
                chaptersQuery = chaptersQuery.Where(ch =>
                    ch.Course != null && (
                        ch.Course.CreateBy == currentUserId!.Value
                        || _context.CourseCollaborators.Any(cc =>
                            cc.CourseId == ch.CourseId
                            && cc.UserId == currentUserId.Value
                            && cc.Status == "Active")));
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
                lessonsQuery = lessonsQuery.Where(l =>
                    l.Chapter != null && l.Chapter.Course != null && (
                        l.Chapter.Course.CreateBy == currentUserId!.Value
                        || _context.CourseCollaborators.Any(cc =>
                            cc.CourseId == l.Chapter.CourseId
                            && cc.UserId == currentUserId.Value
                            && cc.Status == "Active")));
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

                chaptersQuery = chaptersQuery.Where(ch =>
                    ch.Course != null && (
                        ch.Course.CreateBy == currentUserId.Value
                        || _context.CourseCollaborators.Any(cc =>
                            cc.CourseId == ch.CourseId
                            && cc.UserId == currentUserId.Value
                            && cc.Status == "Active")));
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
        public async Task<IActionResult> Create(Lesson lesson, DateTime? ScheduledDateTime, int? DurationMinutes, int? MaxParticipants, bool IsRecordingEnabled, bool IsRecordingPublic)
        {
            if (!CanManageChapter(lesson.ChapterId))
            {
                return Forbid();
            }

            // Quy tắc: chỉ bài học dạng Video mới cần URL video
            if (lesson.Type == "Video" && string.IsNullOrWhiteSpace(lesson.VideoUrl))
            {
                ModelState.AddModelError(nameof(Lesson.VideoUrl), "Vui lòng chọn hoặc nhập URL video cho bài học dạng Video.");
            }

            if (lesson.Type != "Video")
            {
                lesson.VideoUrl = null;
                ModelState.Remove(nameof(Lesson.VideoUrl));
            }

            if (lesson.Type == "LiveClass")
            {
                lesson.Duration = null;
                ModelState.Remove(nameof(Lesson.Duration));
            }

            if (ModelState.IsValid)
            { 
                
                var currentUserId = GetCurrentUserId();
                if (!currentUserId.HasValue)
                {
                    return Forbid();
                }

                // Handle Live Class creation
                if (lesson.Type == "LiveClass")
                {
                    try
                    {
                        // Get the course associated with this lesson's chapter
                        var chapter = _context.Chapters.FirstOrDefault(c => c.Id == lesson.ChapterId);
                        if (chapter == null)
                        {
                            ModelState.AddModelError("", "Chương không tồn tại");
                            return View(lesson);
                        }

                        // Use provided datetime or current time if not provided
                        var scheduledTime = ScheduledDateTime ?? DateTime.Now.AddMinutes(5);

                        // Create LiveClass record
                        var liveClass = new LiveClass
                        {
                            CourseId = chapter.CourseId,
                            Title = lesson.Title,
                            Description = lesson.Description,
                            ScheduledDateTime = scheduledTime,
                            DurationMinutes = DurationMinutes ?? 60,
                            MaxParticipants = MaxParticipants,
                            CreateBy = currentUserId.Value,
                            Status = "Scheduled",
                            IsRecordingEnabled = IsRecordingEnabled,
                            IsRecordingPublic = IsRecordingPublic,
                            CreatedAt = DateTime.Now
                        };

                        _context.LiveClasses.Add(liveClass);
                        _context.SaveChanges();

                        // Try to create Zoom meeting asynchronously
                        try
                        {
                            var meetingRequest = new ZoomMeetingRequest
                            {
                                Topic = lesson.Title,
                                StartTime = scheduledTime,
                                DurationMinutes = DurationMinutes ?? 60,
                                TimeZone = "Asia/Ho_Chi_Minh",
                                RecordingEnabled = IsRecordingEnabled,
                                Password = Guid.NewGuid().ToString().Substring(0, 8)
                            };

                            var meetingResponse = await _zoomService.CreateMeetingAsync(meetingRequest);

                            if (meetingResponse != null)
                            {
                                liveClass.ZoomMeetingId = meetingResponse.MeetingId;
                                liveClass.JoinUrl = meetingResponse.JoinUrl;
                                liveClass.StartUrl = meetingResponse.StartUrl;
                                _context.SaveChanges();

                                // Get enrolled students and send invitation emails
                                var enrolledStudents = _context.Enrollments
                                    .Where(e => e.CourseId == chapter.CourseId)
                                    .Include(e => e.User)
                                    .Select(e => e.User)
                                    .Distinct()
                                    .ToList();

                                if (enrolledStudents.Any())
                                {
                                    var emailTasks = enrolledStudents.Select(student =>
                                        _emailSender.SendEmailAsync(
                                            student.Email ?? "",
                                            $"Mời tham gia lớp học trực tiếp: {lesson.Title}",
                                            GenerateLiveClassInvitationHtml(lesson.Title, scheduledTime, meetingResponse.JoinUrl, student.FullName)
                                        )
                                    );

                                    await Task.WhenAll(emailTasks);
                                }

                                TempData["SuccessMessage"] = $"✅ Lớp học trực tiếp đã được tạo! Email mời đã gửi tới {enrolledStudents.Count()} học viên.";
                            }
                            else
                            {
                                TempData["WarningMessage"] = "⚠️ Lớp học đã được tạo nhưng gặp lỗi khi tạo meeting Zoom. Bạn có thể tạo meeting thủ công sau.";
                            }
                        }
                        catch (Exception zoomEx)
                        {
                            // Log but don't fail - meeting can be created manually later
                            System.Diagnostics.Debug.WriteLine($"Zoom meeting creation failed: {zoomEx.Message}");
                            TempData["WarningMessage"] = "⚠️ Lớp học đã được tạo ra nhưng gặp lỗi khi tạo meeting Zoom. Kiểm tra logs để biết chi tiết.";
                        }

                        // Link the lesson to the live class
                        lesson.LiveClassId = liveClass.Id;
                        lesson.VideoUrl = null; // Live class doesn't need video URL initially
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = "Lỗi tạo lớp học trực tiếp: " + ex.Message;
                        if (ex.InnerException != null)
                        {
                            errorMsg += " | " + ex.InnerException.Message;
                        }
                        ModelState.AddModelError("", errorMsg);
                        return View(lesson);
                    }
                }
                else if (lesson.Type != "LiveClass")
                {
                    lesson.VideoUrl = NormalizeVideoUrlForStorage(lesson.VideoUrl);
                }

                lesson.CreateBy = currentUserId.Value;
                _context.Set<Lesson>().Add(lesson);
                _context.SaveChanges();
                
                // Redirect to lessons index to show success message
                return RedirectToAction("Index", new { courseId = (lesson.Chapter?.CourseId ?? 0) });
            }
            // Nếu lỗi, truyền lại danh sách chương
            var chaptersQuery = _context.Set<Chapter>().AsQueryable();
            var currentUserId2 = GetCurrentUserId();
            if (!IsAdmin() && currentUserId2.HasValue)
            {
                chaptersQuery = chaptersQuery.Where(ch =>
                    ch.Course != null && (
                        ch.Course.CreateBy == currentUserId2.Value
                        || _context.CourseCollaborators.Any(cc =>
                            cc.CourseId == ch.CourseId
                            && cc.UserId == currentUserId2.Value
                            && cc.Status == "Active")));
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
                chaptersQuery = chaptersQuery.Where(ch =>
                    ch.Course != null && (
                        ch.Course.CreateBy == currentUserId.Value
                        || _context.CourseCollaborators.Any(cc =>
                            cc.CourseId == ch.CourseId
                            && cc.UserId == currentUserId.Value
                            && cc.Status == "Active")));
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

            // Quy tắc: chỉ bài học dạng Video mới cần URL video
            if (lesson.Type == "Video" && string.IsNullOrWhiteSpace(lesson.VideoUrl))
            {
                ModelState.AddModelError(nameof(Lesson.VideoUrl), "Vui lòng chọn hoặc nhập URL video cho bài học dạng Video.");
            }

            if (lesson.Type != "Video")
            {
                lesson.VideoUrl = null;
                ModelState.Remove(nameof(Lesson.VideoUrl));
            }

            if (lesson.Type == "LiveClass")
            {
                lesson.Duration = null;
                ModelState.Remove(nameof(Lesson.Duration));
            }

            if (ModelState.IsValid)
            {
                existingLesson.ChapterId = lesson.ChapterId;
                existingLesson.Title = lesson.Title;
                existingLesson.Description = lesson.Description;
                existingLesson.Content = lesson.Content;
                existingLesson.VideoUrl = lesson.Type == "Video"
                    ? NormalizeVideoUrlForStorage(lesson.VideoUrl)
                    : null;
                existingLesson.Duration = lesson.Type == "LiveClass" ? null : lesson.Duration;
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
                chaptersQuery = chaptersQuery.Where(ch =>
                    ch.Course != null && (
                        ch.Course.CreateBy == currentUserId2.Value
                        || _context.CourseCollaborators.Any(cc =>
                            cc.CourseId == ch.CourseId
                            && cc.UserId == currentUserId2.Value
                            && cc.Status == "Active")));
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

            return _context.Courses.Any(c =>
                c.Id == courseId
                && (c.CreateBy == currentUserId.Value
                    || _context.CourseCollaborators.Any(cc =>
                        cc.CourseId == c.Id
                        && cc.UserId == currentUserId.Value
                        && cc.Status == "Active")));
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

            return _context.Chapters.Any(ch =>
                ch.Id == chapterId
                && ch.Course != null
                && (ch.Course.CreateBy == currentUserId.Value
                    || _context.CourseCollaborators.Any(cc =>
                        cc.CourseId == ch.CourseId
                        && cc.UserId == currentUserId.Value
                        && cc.Status == "Active")));
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

            return _context.Lessons.Any(l =>
                l.Id == lessonId
                && l.Chapter != null
                && l.Chapter.Course != null
                && (l.Chapter.Course.CreateBy == currentUserId.Value
                    || _context.CourseCollaborators.Any(cc =>
                        cc.CourseId == l.Chapter.CourseId
                        && cc.UserId == currentUserId.Value
                        && cc.Status == "Active")));
        }

        private string GenerateLiveClassInvitationHtml(string lessonTitle, DateTime scheduledTime, string joinUrl, string studentName)
        {
            return $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <h2>Mời tham gia lớp học trực tiếp</h2>
                <p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(studentName)}</strong>,</p>
                
                <p>Bạn được mời tham gia lớp học trực tiếp:</p>
                <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #007bff;'>
                    <h3 style='color: #007bff; margin-top: 0;'>{System.Net.WebUtility.HtmlEncode(lessonTitle)}</h3>
                    <p><strong>Thời gian:</strong> {scheduledTime:dd/MM/yyyy HH:mm} (múi giờ VN)</p>
                </div>
                
                <p>Nhấp vào nút dưới để tham gia lớp học:</p>
                <p>
                    <a href='{joinUrl}' style='background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                        Tham gia Lớp Học Trực Tiếp
                    </a>
                </p>
                
                <p>Hoặc sao chép link dưới vào trình duyệt:</p>
                <p style='background-color: #f9f9f9; padding: 10px; word-break: break-all;'>{joinUrl}</p>
                
                <p>Hẹn gặp bạn!</p>
            </div>";
        }

    }
}
