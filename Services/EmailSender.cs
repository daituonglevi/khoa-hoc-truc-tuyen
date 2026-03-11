using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using ELearningWebsite.Models;

namespace ELearningWebsite.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                _logger.LogInformation("Starting to send email to {Email}", email);
                _logger.LogInformation("SMTP Server: {Server}:{Port}", _emailSettings.SmtpServer, _emailSettings.SmtpPort);
                _logger.LogInformation("Sender: {SenderEmail}", _emailSettings.SenderEmail);

                var smtpClient = new SmtpClient(_emailSettings.SmtpServer)
                {
                    Port = _emailSettings.SmtpPort,
                    Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password),
                    EnableSsl = true,
                    Timeout = 30000 // 30 seconds timeout
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email);

                _logger.LogInformation("Attempting to send email...");
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {Email}", email);

                // Dispose resources
                mailMessage.Dispose();
                smtpClient.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}. Error: {ErrorMessage}", email, ex.Message);
                throw;
            }
        }
    }
}
