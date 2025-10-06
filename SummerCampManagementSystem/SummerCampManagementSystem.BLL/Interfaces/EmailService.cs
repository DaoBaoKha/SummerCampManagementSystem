using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security; 
using SummerCampManagementSystem.Core.Config;

namespace SummerCampManagementSystem.BLL.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);

        Task SendOtpEmailAsync(string to, string otp);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSetting _emailSetting;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSetting> emailSetting, ILogger<EmailService> logger)
        {
            _emailSetting = emailSetting.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_emailSetting.SenderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_emailSetting.SmtpServer, _emailSetting.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSetting.SenderEmail, _emailSetting.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending email to {Recipient}", to);
                throw;
            }
        }

        public async Task SendOtpEmailAsync(string to, string otp)
        {
            string subject = "Xác Nhận Tài Khoản Summer Camp";
            string body = $"<p>Mã OTP của bạn là: <strong>{otp}</strong></p><p>Vui lòng không chia sẻ mã này với bất kỳ ai.</p>";   

            await SendEmailAsync(to, subject, body);
        }
    }
}