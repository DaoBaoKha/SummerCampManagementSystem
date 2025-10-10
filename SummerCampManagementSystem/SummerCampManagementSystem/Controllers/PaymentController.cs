using Microsoft.AspNetCore.Http;
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
    }
}
