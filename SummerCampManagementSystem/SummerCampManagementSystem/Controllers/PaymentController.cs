using Microsoft.AspNetCore.Mvc;
using SummerCampManagementSystem.BLL.DTOs.PayOS;
using SummerCampManagementSystem.BLL.Interfaces;

namespace SummerCampManagementSystem.API.Controllers
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ITransactionService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentController(ITransactionService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService;
            _configuration = configuration;
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

        [HttpGet("mobile-callback")]
        public IActionResult PaymentMobileCallback()
        {
            try
            {
                // take all query string
                string rawQueryString = Request.QueryString.Value ?? string.Empty;


                string deepLinkUrl = _paymentService.ProcessPaymentMobileCallbackRaw(rawQueryString);

                // do 302 redirect
                return Redirect(deepLinkUrl);
            }
            catch (ArgumentException ex)
            {
                // validation errors
                string errorReason = Uri.EscapeDataString(ex.Message);
                return Redirect($"yourapp://payment/failure?reason=Validation&details={errorReason}");
            }
            catch (Exception ex)
            {
                // exceptions
                string errorReason = Uri.EscapeDataString(ex.Message);
                const string fallbackDeepLink = "yourapp://payment/failure?reason=API_ERROR";
                return Redirect(fallbackDeepLink + $"&details={errorReason}");
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

                var resultDto = await _paymentService.ProcessPaymentWebsiteCallbackRaw(rawQueryString);

                return Ok(resultDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Lỗi hệ thống khi xử lý callback.", detail = ex.Message });
            }
        }
    }
}
