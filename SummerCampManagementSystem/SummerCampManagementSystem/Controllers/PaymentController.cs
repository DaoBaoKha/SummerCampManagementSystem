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

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
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
    }
}
