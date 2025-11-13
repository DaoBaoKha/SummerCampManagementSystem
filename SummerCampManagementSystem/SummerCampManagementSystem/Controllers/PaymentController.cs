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

        private const string BaseDeepLink = "summercamp://payment";

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
        [ProducesResponseType(StatusCodes.Status302Found)] // this is a redirect
        public async Task<IActionResult> PaymentMobileCallback()
        {
            string deepLinkUrl;

            try
            {
                string rawQueryString = Request.QueryString.Value ?? string.Empty;
                _logger.LogInformation($"Mobile Callback: Nhận được query: {rawQueryString}");

                // service return the deep link to redirect
                deepLinkUrl = await _paymentService.ProcessPaymentMobileCallbackRaw(rawQueryString);

                _logger.LogInformation($"Mobile Callback: Xử lý thành công, redirect về: {deepLinkUrl}");

                return Redirect(deepLinkUrl);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, $"Mobile Callback Validation Error: {ex.Message}");

                // failure deep link with reason and details
                deepLinkUrl = $"{BaseDeepLink}/failure?reason=Validation&details={Uri.EscapeDataString(ex.Message)}";
                return Redirect(deepLinkUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Mobile Callback Exception: {ex.Message}");

                // failure deep link with reason and details
                deepLinkUrl = $"{BaseDeepLink}/failure?reason=ApiError&details={Uri.EscapeDataString(ex.Message)}";
                return Redirect(deepLinkUrl);
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
