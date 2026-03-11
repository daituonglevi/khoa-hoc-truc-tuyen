using Microsoft.AspNetCore.Mvc;
using ELearningWebsite.Services;
using Microsoft.Extensions.Options;
using ELearningWebsite.Models;

namespace ELearningWebsite.Controllers
{
    public class TestEmailController : Controller
    {
        private readonly IEmailSender _emailSender;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<TestEmailController> _logger;

        public TestEmailController(
            IEmailSender emailSender, 
            IOptions<EmailSettings> emailSettings,
            ILogger<TestEmailController> logger)
        {
            _emailSender = emailSender;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        // GET: /TestEmail
        public IActionResult Index()
        {
            ViewBag.EmailSettings = _emailSettings;
            return View();
        }

        // POST: /TestEmail/Send
        [HttpPost]
        public async Task<IActionResult> Send(string testEmail)
        {
            try
            {
                if (string.IsNullOrEmpty(testEmail))
                {
                    ViewBag.Error = "Vui lòng nhập email đ�f test";
                    ViewBag.EmailSettings = _emailSettings;
                    return View("Index");
                }

                _logger.LogInformation("Testing email send to: {Email}", testEmail);

                await _emailSender.SendEmailAsync(
                    testEmail,
                    "Test Email - ELearning CNTT",
                    @"
                    <h2>�YZ? Test Email Thành Công!</h2>
                    <p>Nếu bạn nhận được email này, nghĩa là cấu hình email đã hoạt đ�Tng đúng.</p>
                    <p><strong>Thời gian gửi:</strong> " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + @"</p>
                    <hr>
                    <p style='color: #666; font-size: 12px;'>Email này được gửi từ h�? thđng test ELearning CNTT</p>
                    ");

                ViewBag.Success = $"Email test đã được gửi thành công đến {testEmail}. Vui lòng ki�fm tra h�Tp thư (bao gôm cả spam).";
                _logger.LogInformation("Test email sent successfully to: {Email}", testEmail);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"L�-i gửi email: {ex.Message}";
                _logger.LogError(ex, "Failed to send test email to: {Email}", testEmail);
            }

            ViewBag.EmailSettings = _emailSettings;
            return View("Index");
        }
    }
}
