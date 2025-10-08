using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
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

            // **FIX: Validate email settings on initialization**
            ValidateEmailSettings();
        }

        private void ValidateEmailSettings()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(_emailSetting.SmtpServer))
                errors.Add("SmtpServer is null or empty");

            if (_emailSetting.Port <= 0)
                errors.Add($"Port is invalid: {_emailSetting.Port}");

            if (string.IsNullOrWhiteSpace(_emailSetting.SenderName))
                errors.Add("SenderName is null or empty");

            if (string.IsNullOrWhiteSpace(_emailSetting.SenderEmail))
                errors.Add("SenderEmail is null or empty");

            if (string.IsNullOrWhiteSpace(_emailSetting.Password))
                errors.Add("Password is null or empty");

            if (errors.Any())
            {
                var errorMessage = "Email configuration is incomplete:\n" + string.Join("\n", errors.Select(e => $"   - {e}"));
                Console.WriteLine(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            Console.WriteLine("Email settings validated successfully:");
            Console.WriteLine($"   - SMTP Server: {_emailSetting.SmtpServer}");
            Console.WriteLine($"   - Port: {_emailSetting.Port}");
            Console.WriteLine($"   - Sender Name: {_emailSetting.SenderName}");
            Console.WriteLine($"   - Sender Email: {_emailSetting.SenderEmail}");
            Console.WriteLine($"   - Password length: {_emailSetting.Password?.Length ?? 0}");
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                // **FIX: Validate parameters**
                if (string.IsNullOrWhiteSpace(to))
                    throw new ArgumentNullException(nameof(to), "Recipient email cannot be null or empty");

                if (string.IsNullOrWhiteSpace(subject))
                    throw new ArgumentNullException(nameof(subject), "Email subject cannot be null or empty");

                if (string.IsNullOrWhiteSpace(body))
                    throw new ArgumentNullException(nameof(body), "Email body cannot be null or empty");

                Console.WriteLine($"Preparing to send email to: {to}");
                Console.WriteLine($"   Subject: {subject}");

                var message = new MimeMessage();

                // **FIX: Add sender with explicit validation**
                message.From.Add(new MailboxAddress(_emailSetting.SenderName, _emailSetting.SenderEmail));
                Console.WriteLine($"   From: {_emailSetting.SenderName} <{_emailSetting.SenderEmail}>");

                // **FIX: Add recipient with explicit validation**
                message.To.Add(MailboxAddress.Parse(to));
                Console.WriteLine($"   To: {to}");

                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                Console.WriteLine($"🔌 Connecting to SMTP server: {_emailSetting.SmtpServer}:{_emailSetting.Port}");
                await client.ConnectAsync(_emailSetting.SmtpServer, _emailSetting.Port, SecureSocketOptions.StartTls);

                Console.WriteLine($"🔐 Authenticating with email: {_emailSetting.SenderEmail}");
                await client.AuthenticateAsync(_emailSetting.SenderEmail, _emailSetting.Password);

                Console.WriteLine($"📤 Sending email...");
                await client.SendAsync(message);

                await client.DisconnectAsync(true);

                Console.WriteLine($"✓ Email sent successfully to: {to}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to send email to {to}: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task SendOtpEmailAsync(string to, string otp, string purpose)
        {
            // **FIX: Validate inputs**
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentNullException(nameof(to), "Recipient email cannot be null or empty");

            if (string.IsNullOrWhiteSpace(otp))
                throw new ArgumentNullException(nameof(otp), "OTP cannot be null or empty");

            string subject = purpose switch
            {
                "Activation" => "Mã OTP Kích Hoạt Tài Khoản",
                "ResetPassword" => "Mã OTP Đặt Lại Mật Khẩu",
                _ => "Mã OTP Xác Thực"
            };

            string body = purpose switch
            {
                "Activation" => $@"
                    <html>
                    <body>
                        <h2>Xin chào!</h2>
                        <p>Mã OTP để kích hoạt tài khoản của bạn là:</p>
                        <h1 style='color: #4CAF50; font-size: 32px;'>{otp}</h1>
                        <p>Mã này có hiệu lực trong 5 phút.</p>
                        <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                        <br>
                        <p>Trân trọng,<br>CampEase Team</p>
                    </body>
                    </html>",

                "ResetPassword" => $@"
                    <html>
                    <body>
                        <h2>Xin chào!</h2>
                        <p>Bạn đã yêu cầu đặt lại mật khẩu. Mã OTP của bạn là:</p>
                        <h1 style='color: #FF5722; font-size: 32px;'>{otp}</h1>
                        <p>Mã này có hiệu lực trong 5 phút.</p>
                        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này và bảo mật tài khoản của bạn.</p>
                        <br>
                        <p>Trân trọng,<br>CampEase Team</p>
                    </body>
                    </html>",

                _ => $@"
                    <html>
                    <body>
                        <h2>Mã OTP Xác Thực</h2>
                        <p>Mã OTP của bạn là:</p>
                        <h1 style='color: #2196F3; font-size: 32px;'>{otp}</h1>
                        <p>Mã này có hiệu lực trong 5 phút.</p>
                        <br>
                        <p>Trân trọng,<br>CampEase Team</p>
                    </body>
                    </html>"
            };

            Console.WriteLine($"📨 Sending OTP email for {purpose} to: {to}");
            await SendEmailAsync(to, subject, body);
        }
    }
}