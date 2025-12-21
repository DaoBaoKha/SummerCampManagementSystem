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

        Task SendAccountCreatedEmailAsync(string toEmail, string role);

        Task SendEmailUpdateSuccessAsync(string newEmail, string oldEmail);

        Task SendLevel3ReportNotificationAsync(string toEmail, string camperName, string reportType, string note, string imageUrl);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSetting _emailSetting;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSetting> emailSetting, ILogger<EmailService> logger)
        {
            _emailSetting = emailSetting.Value;

            _emailSetting.SmtpServer = _emailSetting.SmtpServer?.Trim() ?? string.Empty;
            _emailSetting.SenderEmail = _emailSetting.SenderEmail?.Trim() ?? string.Empty;
            _emailSetting.Password = _emailSetting.Password?.Trim() ?? string.Empty;

            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // check for config (fail fast)
            if (string.IsNullOrWhiteSpace(_emailSetting.SmtpServer))
            {
                _logger.LogError("CRITICAL CONFIG: EmailSetting__SmtpServer is null or empty. Cannot connect to SMTP.");
                throw new InvalidOperationException("Email server configuration is missing. SmtpServer is null or empty.");
            }

            if (string.IsNullOrWhiteSpace(_emailSetting.Password))
            {
                _logger.LogError("CRITICAL CONFIG: EmailSetting__Password is null or empty. Cannot authenticate to SMTP.");
                throw new InvalidOperationException("Email server configuration is missing. Password is null or empty.");
            }

            _logger.LogInformation(
                "DEBUG: Attempting SMTP connection. Server: '{SmtpServer}', Port: {Port}, Sender: '{SenderEmail}'",
                _emailSetting.SmtpServer,
                _emailSetting.Port,
                _emailSetting.SenderEmail
            );

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
                // invalid uri error here
                await smtp.ConnectAsync(_emailSetting.SmtpServer, _emailSetting.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSetting.SenderEmail, _emailSetting.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                
                _logger.LogInformation("Email sent successfully to {Recipient}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                                 "FATAL ERROR: Failed to connect or send email to {Recipient}. Config: Server='{Server}', Port={Port}",
                                 to, _emailSetting.SmtpServer, _emailSetting.Port);
                throw;
            }
        }

        public async Task SendAccountCreatedEmailAsync(string toEmail, string role)
        {
            var subject = "Tài khoản CampEase đã được tạo";
            var body = $"Xin chào,\n\nTài khoản {role} của bạn đã được tạo trong hệ thống CampEase.\nVui lòng đăng nhập và đổi mật khẩu.\n\nTrân trọng,\nĐội ngũ CampEase";
            await SendEmailAsync(toEmail, subject, body);
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
            else if (purpose.Equals("EmailUpdate", StringComparison.OrdinalIgnoreCase))
            {
                subject = "Xác nhận địa chỉ Email mới Summer Camp";
                body = $@"
            <p>Xin chào,</p>
            <p>Mã OTP của bạn để <strong>xác nhận địa chỉ email mới</strong> là: 
            <strong style='color:#3498DB;font-size:20px'>{otp}</strong></p>
            <p>Mã này có hiệu lực trong 5 phút. Vui lòng nhập mã này để hoàn tất việc thay đổi email.</p>
            <p>Trân trọng,<br/>Summer Camp Team</p>";
            }
            else
            {
                subject = "Mã Xác Thực OTP";
                body = $"<p>Mã OTP của bạn là: <strong>{otp}</strong></p>";
            }

            await SendEmailAsync(to, subject, body);
        }

 
        public async Task SendEmailUpdateSuccessAsync(string newEmail, string oldEmail)
        {
            // send to new email (success)
            var newEmailSubject = "Cập nhật Email Tài khoản thành công!";
            var newEmailBody = $@"
            <p>Xin chào,</p>
            <p>Email đăng nhập của bạn đã được thay đổi thành công sang địa chỉ này (<strong>{newEmail}</strong>).</p>
            <p>Kể từ bây giờ, vui lòng sử dụng địa chỉ email này để đăng nhập vào hệ thống.</p>
            <p>Trân trọng,<br/>Summer Camp Team</p>";
            await SendEmailAsync(newEmail, newEmailSubject, newEmailBody);


            // send to old email (alert)
            var oldEmailSubject = "Cảnh báo: Địa chỉ Email Tài khoản đã bị thay đổi";
            var oldEmailBody = $@"
            <p>Xin chào,</p>
            <p>Chúng tôi thông báo rằng địa chỉ email liên kết với tài khoản của bạn đã được thay đổi từ <strong>{oldEmail}</strong> sang <strong>{newEmail}</strong>.</p>
            <p style='color:red;'>Nếu bạn không thực hiện việc thay đổi này, vui lòng liên hệ ngay với bộ phận hỗ trợ của chúng tôi để bảo vệ tài khoản của bạn.</p>
            <p>Trân trọng,<br/>Summer Camp Team</p>";
            await SendEmailAsync(oldEmail, oldEmailSubject, oldEmailBody);
        }

        public async Task SendLevel3ReportNotificationAsync(string toEmail, string camperName, string reportType, string note, string imageUrl)
        {
            _logger.LogInformation("Preparing to send Level 3 report notification to {Email} for camper {CamperName}", toEmail, camperName);
            
            var subject = $"Thông báo quan trọng về {camperName} - Báo cáo Mức 3";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #E74C3C;'>⚠️ Thông báo quan trọng</h2>
                <p>Xin chào Quý Phụ huynh,</p>
                <p>Chúng tôi xin thông báo về một sự việc <strong>mức độ 3</strong> liên quan đến con em của Quý vị: <strong>{camperName}</strong></p>
                
                <div style='background-color: #f8f9fa; padding: 15px; border-left: 4px solid #E74C3C; margin: 20px 0;'>
                    <p><strong>Loại báo cáo:</strong> {reportType}</p>
                    <p><strong>Chi tiết:</strong> {note}</p>
                </div>
                
                {(!string.IsNullOrEmpty(imageUrl) ? $"<p><strong>Hình ảnh đính kèm:</strong></p><img src='{imageUrl}' alt='Report Image' style='max-width: 100%; height: auto; border-radius: 8px;' />" : "")}
                
                <p style='margin-top: 20px;'>Nếu Quý vị có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi ngay.</p>
                <p>Trân trọng,<br/>Đội ngũ Summer Camp</p>
            </div>";
            
            await SendEmailAsync(toEmail, subject, body);
            
            _logger.LogInformation("Level 3 report notification sent successfully to {Email}", toEmail);
        }
    }
}
