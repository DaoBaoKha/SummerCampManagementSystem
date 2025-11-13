using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.PayOS;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("payos-webhook")]
        public async Task<IActionResult> PayOSWebhook([FromBody] PayOSWebhookRequestDto webhookRequest)
        {
            try
            {
                // move all received data to service layer for processing
                await _paymentService.HandlePayOSWebhook(webhookRequest);

                // return 200 OK to acknowledge receipt of the webhook
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing PayOS webhook: {ex.Message}");

                // payos might retry sending the webhook if we return a failure status code
                return BadRequest(new { message = "An error occurred while processing the webhook." });
            }
        }

        // MOBILE CALLBACK
        // redirect user to deep link after processing
        [HttpGet("mobile-callback")]
        public async Task<IActionResult> PaymentMobileCallback()
        {
            string deepLinkUrl = string.Empty;

            try
            {
                string rawQueryString = Request.QueryString.Value ?? string.Empty;
                _logger.LogInformation($"Mobile Callback: Nhận được query: {rawQueryString}");

               
                deepLinkUrl = await _paymentService.ProcessPaymentMobileCallbackRaw(rawQueryString);

                _logger.LogInformation($"Mobile Callback: Xử lý thành công, redirect về: {deepLinkUrl}");

                string html = $@"
                    <html>
                        <head>
                            <meta http-equiv='refresh' content='0; url={deepLinkUrl}' />
                            <title>Redirecting...</title>
                        </head>
                        <body>
                            <p>Redirecting to your app...</p>
                            <p>If not redirected, <a href='{deepLinkUrl}'>click here</a>.</p>
                        </body>
                    </html>";

                return Content(html, "text/html");
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, $"Mobile Callback Validation Error: {ex.Message}");

                deepLinkUrl = $"yourapp://payment/failure?reason=Validation&details={Uri.EscapeDataString(ex.Message)}";

                string html = $@"
                    <html>
                        <head>
                            <meta http-equiv='refresh' content='0; url={deepLinkUrl}' />
                            <title>Redirecting...</title>
                        </head>
                        <body>
                            <p>Redirecting to your app...</p>
                            <p>If not redirected, <a href='{deepLinkUrl}'>click here</a>.</p>
                        </body>
                    </html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Mobile Callback Exception: {ex.Message}");

                deepLinkUrl = $"yourapp://payment/failure?reason=ApiError&details={Uri.EscapeDataString(ex.Message)}";

                string html = $@"
                <html>
                    <head>
                        <meta http-equiv='refresh' content='0; url={deepLinkUrl}' />
                        <title>Redirecting...</title>
                    </head>
                    <body>
                        <p>Redirecting to your app...</p>
                        <p>If not redirected, <a href='{deepLinkUrl}'>click here</a>.</p>
                    </body>
                </html>";

                return Content(html, "text/html");
            }
        }



        [HttpGet("confirm-urls")]
        public async Task<IActionResult> ConfirmPayOSUrls()
        {
            _logger.LogInformation("--- BẮT ĐẦU CHẠY CONFIRM-URLS ---");
            try
            {
                _logger.LogInformation("Confirm: Đang lấy ApiBaseUrl...");

                string baseApiUrl = _configuration["ApiBaseUrl"]
                           ?? throw new InvalidOperationException("ApiBaseUrl is not configured.");

                _logger.LogInformation($"Confirm: ApiBaseUrl = {baseApiUrl}");

                string webhookUrl = $"{baseApiUrl}/api/payment/payos-webhook";

                _logger.LogInformation($"Confirm: Đang gửi xác nhận Webhook URL đến PayOS: {webhookUrl}");

                string webhookResult = await _paymentService.ConfirmUrlAsync(webhookUrl);

                _logger.LogInformation($"Confirm: PayOS trả về cho Webhook = {webhookResult}");

                _logger.LogInformation("--- XÁC NHẬN HOÀN TẤT ---");

                return Ok(new
                {
                    message = "PayOS Webhook URL confirmation processed.",
                    webhook_confirmation = new { url = webhookUrl, result = webhookResult }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "--- LỖI 500 KHI CHẠY CONFIRM-URLS ---");
                // Trả về lỗi chi tiết nếu có
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error confirming PayOS URL(s).", detail = ex.Message });
            }
        }

        [HttpGet("website-callback")]
        public async Task<IActionResult> PaymentWebsiteCallback()
        {
            try
            {
                string rawQueryString = Request.QueryString.Value ?? string.Empty;
                _logger.LogInformation($"Website Callback: Nhận được query: {rawQueryString}"); // log when start processing

                var resultDto = await _paymentService.ProcessPaymentWebsiteCallbackRaw(rawQueryString);

                _logger.LogInformation($"Website Callback: Xử lý thành công, kết quả: {resultDto.Status} - {resultDto.Message}"); // log when success
                return Ok(resultDto);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, $"Website Callback LỖI 1 (ArgumentException): {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Website Callback LỖI 2 (Exception): {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi hệ thống khi xử lý callback.", detail = ex.Message });
            }
        }
    }
}
