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

        Task SendOtpEmailAsync(string to, string otp, string purpose);
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

        public async Task SendOtpEmailAsync(string to, string otp, string purpose)
        {
            string subject;
            string body;

            if (purpose.Equals("Activation", StringComparison.OrdinalIgnoreCase))
            {
                subject = "Xác Nhận Tài Khoản Summer Camp";
                body = $@"
            <p>Xin chào,</p>
            <p>Mã OTP của bạn để <strong>kích hoạt tài khoản</strong> là: 
            <strong style='color:#2E86DE;font-size:20px'>{otp}</strong></p>
            <p>Mã này có hiệu lực trong 5 phút. 
            Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
            <p>Trân trọng,<br/>Summer Camp Team</p>";
            }
            else if (purpose.Equals("ResetPassword", StringComparison.OrdinalIgnoreCase))
            {
                subject = "Đặt Lại Mật Khẩu Summer Camp";
                body = $@"
            <p>Xin chào,</p>
            <p>Bạn đã yêu cầu đặt lại mật khẩu. Mã OTP để <strong>đặt lại mật khẩu</strong> là:
            <strong style='color:#E67E22;font-size:20px'>{otp}</strong></p>
            <p>Mã này có hiệu lực trong 5 phút. 
            Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>
            <p>Trân trọng,<br/>Summer Camp Team</p>";
            }
            else
            {
                subject = "Mã Xác Thực OTP";
                body = $"<p>Mã OTP của bạn là: <strong>{otp}</strong></p>";
            }

            await SendEmailAsync(to, subject, body);
        }
    }
}