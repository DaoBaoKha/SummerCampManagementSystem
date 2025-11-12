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
            try
            {
                string rawQueryString = Request.QueryString.Value ?? string.Empty;
                _logger.LogInformation($"Mobile Callback: Nhận được query: {rawQueryString}"); // log when start processing

                string deepLinkUrl = await _paymentService.ProcessPaymentMobileCallbackRaw(rawQueryString);

                _logger.LogInformation($"Mobile Callback: Xử lý thành công, redirect về: {deepLinkUrl}"); // log when success
                return Redirect(deepLinkUrl);
            }
            catch (ArgumentException ex) 
            {
                _logger.LogError(ex, $"Mobile Callback LỖI 1 (ArgumentException): {ex.Message}");

                string errorReason = Uri.EscapeDataString(ex.Message);
                return Redirect($"yourapp://payment/failure?reason=Validation&details={errorReason}");
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, $"Mobile Callback LỖI 2 (Exception): {ex.Message}");

                string errorReason = Uri.EscapeDataString(ex.Message);
                return Redirect($"yourapp://payment/failure?reason=ApiError&details={errorReason}");
            }
        }

        [HttpGet("confirm-urls")]
        public async Task<IActionResult> ConfirmPayOSUrls()
        {
            try
            {
                // take url callback from config on gg cloud
                string returnUrl = _configuration["PayOS:ReturnUrl"] ??
                                   throw new InvalidOperationException("PayOS:ReturnUrl is not configured.");

                string result = await _paymentService.ConfirmUrlAsync(returnUrl);

                // PayOS SDK return confirm result
                return Ok(new
                {
                    message = "PayOS URL confirmed successfully.",
                    urlConfirmed = returnUrl,
                    payOSResult = result
                });
            }
            catch (Exception ex)
            {
                // exception if payos deny (like not HTTPS)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error confirming PayOS URL. Check console log for detail.", detail = ex.Message });
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
